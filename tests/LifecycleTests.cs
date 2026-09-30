using System.Text.Json.Nodes;
using BD2SecretVision;
internal static class LifecycleTests
{
    public static async Task<int> Run()
    {
        int count=0;void Check(bool ok,string why){count++;if(!ok)throw new Exception("Lifecycle: "+why);}
        async Task<(Automation Runner,FakeGame Game,string Root)> Case(Settings settings,params string[] outcomes){
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,outcomes);var connection=new Connection(root);
            var runner=new Automation(connection,()=>game.Process,(_,owner,_)=>{game.Owner=owner;return game;},(port,ct)=>game.Play(ct),(_,_)=>Task.CompletedTask);
            await runner.Run(settings,CancellationToken.None);return(runner,game,root);
        }
        var success=await Case(new Settings{Stages=[1,3]},"pass","pass");
        Check(success.Runner.Progress.Status=="completed"&&success.Runner.Progress.Cleared.SequenceEqual(new[]{1,3}),"selected stages complete in order");
        Check(success.Game.Starts==2&&success.Game.Exits==2,"one start per stage and native exit");
        Check(!Files.Read<JsonNode>(Path.Combine(success.Root,"control.json")).B("Enabled"),"completion revokes lease");
        Check(Directory.GetFiles(success.Root,"result.json",SearchOption.AllDirectories).Length==2,"each attempt has durable evidence");
        {
            // A confirmed result is durable and visible before either next-stage or final exit.
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass","pass"]);
            var entered=new[]{new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)};
            var release=new[]{new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)};
            int waits=0;var durations=new List<TimeSpan>();
            var runner=new Automation(new Connection(root),()=>game.Process,(_,_,_)=>game,(_,ct)=>game.Play(ct),async(duration,ct)=>{
                int index=waits++;durations.Add(duration);entered[index].SetResult();await release[index].Task.WaitAsync(ct);
            });
            var running=runner.Run(new Settings{Stages=[1,3]},CancellationToken.None);
            for(int i=0;i<2;i++){
                await entered[i].Task.WaitAsync(TimeSpan.FromSeconds(5));
                Check(!running.IsCompleted&&game.Starts==i+1&&game.Exits==i,"success screen held before next start or exit");
                Check(runner.Progress.Cleared.Count==i+1,"success recorded before display delay");
                Check(Directory.GetFiles(root,"result.json",SearchOption.AllDirectories).Length==i+1,"evidence saved before display delay");
                Check(durations[i]==TimeSpan.FromSeconds(8),"success display lasts eight seconds");
                release[i].SetResult();
            }
            await running.WaitAsync(TimeSpan.FromSeconds(5));
            Check(waits==2&&game.Starts==2&&game.Exits==2&&runner.Progress.Status=="completed","normal progression resumes after success holds");
        }
        {
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass","pass"]);using var cancellation=new CancellationTokenSource();
            var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var runner=new Automation(new Connection(root),()=>game.Process,(_,_,_)=>game,(_,ct)=>game.Play(ct),async(_,ct)=>{
                entered.SetResult();await Task.Delay(Timeout.InfiniteTimeSpan,ct);
            });
            var running=runner.Run(new Settings{Stages=[1,2]},cancellation.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));cancellation.Cancel();
            await running.WaitAsync(TimeSpan.FromSeconds(5));
            Check(runner.Progress.Status=="stopped"&&game.Starts==1&&game.Exits==0,"stop interrupts success hold without next-stage or exit commands");
            Check(runner.Progress.Cleared.SequenceEqual(new[]{1}),"stop during display preserves confirmed success");
            Check(!Files.Read<JsonNode>(Path.Combine(root,"control.json")).B("Enabled"),"stop during display revokes control");
        }
        var retry=await Case(new Settings{Stages=[2],MaxAttempts=3},"fail","pass");
        Check(retry.Runner.Progress.Status=="completed"&&retry.Game.Starts==2,"failed round retried then stopped on success");
        Check(retry.Game.Rounds.Distinct().Count()==2,"distinct identity per attempt");
        var noRetry=await Case(new Settings{Retry=false},"fail","pass");
        Check(noRetry.Runner.Progress.Status=="attempted"&&noRetry.Game.Starts==1,"retry disabled");
        var limited=await Case(new Settings{MaxAttempts=2},"fail","fail","pass");
        Check(limited.Runner.Progress.Status=="attempted"&&limited.Game.Starts==2,"maximum attempts honored");
        foreach(var outcome in new[]{"server-missing","server-old","local-old","unknown-command"}){
            var result=await Case(new Settings{MaxAttempts=1},outcome);
            Check(result.Runner.Progress.Status=="blocked"&&result.Runner.Progress.Cleared.Count==0,"uncertain evidence blocks: "+outcome);
            Check(result.Game.Starts==1&&!Files.Read<JsonNode>(Path.Combine(result.Root,"control.json")).B("Enabled"),"no blind replay: "+outcome);
        }
        {
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));var game=new FakeGame(root,["pass"]);var c=new Connection(root);
            using var cancellation=new CancellationTokenSource();
            var runner=new Automation(c,()=>game.Process,(_,_,_)=>game,async(_,ct)=>{cancellation.Cancel();await Task.Delay(1000,ct);});
            await runner.Run(new(),cancellation.Token);
            Check(runner.Progress.Status=="stopped"&&game.Starts==1,"manual cancellation stops immediately");
            Check(!Files.Read<JsonNode>(Path.Combine(root,"control.json")).B("Enabled"),"manual cancellation revokes lease");
            using(var other=new FileStream(Path.Combine(root,"runner.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
                bool rejected=false;try{await runner.Run(new(),CancellationToken.None);}catch(IOException){rejected=true;}
                Check(rejected&&game.Starts==1,"concurrent runner rejected without commands");
            }
        }
        {
            // A locked diagnostic file cannot abort a successful round.
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass"]);var connection=new Connection(root);
            Files.Write(Path.Combine(root,"progress.json"),new{Status="old"});
            using var locked=new FileStream(Path.Combine(root,"progress.json"),FileMode.Open,FileAccess.Read,FileShare.Read);
            var runner=new Automation(connection,()=>game.Process,(_,_,_)=>game,(_,ct)=>game.Play(ct),(_,_)=>Task.CompletedTask);
            await runner.Run(new(),CancellationToken.None);
            Check(runner.Progress.Status=="completed"&&runner.Progress.Cleared.SequenceEqual(new[]{1}),"progress-file failure cannot stop gameplay");
            Check(runner.Progress.Warning.Contains("progress.json"),"diagnostic write warning visible without replacing result");
            Check(File.ReadAllText(Path.Combine(root,"diagnostics.jsonl")).Contains("publish file"),"full file failure recorded");
        }
        {
            // A locked legacy control file must not interrupt the live pipe heartbeat.
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass"]);var connection=new Connection(root);
            var runner=new Automation(connection,()=>game.Process,(_,_,_)=>game,async(_,ct)=>{
                using var locked=new FileStream(Path.Combine(root,"control.json"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                await Task.Delay(1200,ct);await game.Play(ct);
            });
            await runner.Run(new(),CancellationToken.None);
            Check(runner.Progress.Status=="completed","locked legacy file cannot break pipe heartbeat");
            Check(game.Starts==1&&!Files.Read<JsonNode>(Path.Combine(root,"control.json")).B("Enabled"),"completed round revokes pipe lease despite old file lock");
        }
        {
            // Revocation failure is secondary; retain the actual gameplay error.
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass"]);FileStream? held=null;
            var runner=new Automation(new Connection(root),()=>game.Process,(_,_,_)=>game,(_,_)=>{
                held=new FileStream(Path.Combine(root,"control.json"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                throw new InvalidOperationException("original planner failure");
            });
            try{await runner.Run(new(),CancellationToken.None);}finally{held?.Dispose();}
            Check(runner.Progress.Detail.Contains("original planner failure")&&!runner.Progress.Warning.Contains("control.json"),"legacy file lock cannot prevent revocation or overwrite first failure");
        }
        foreach(var reason in new[]{"game-Paused","resume-timeout"}){
            string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
            var game=new FakeGame(root,["pass","pass"]);
            var runner=new Automation(new Connection(root),()=>game.Process,(_,_,_)=>game,(_,_)=>throw new RouteFailure(reason));
            await runner.Run(new Settings{Retry=true,MaxAttempts=0},CancellationToken.None);
            Check(runner.Progress.Status=="blocked"&&game.Starts==1&&game.Exits==0,"control error never loops into another round: "+reason);
            Check(!Files.Read<JsonNode>(Path.Combine(root,"control.json")).B("Enabled"),"control error revokes owner: "+reason);
        }
        var languages=new[]{"zh-CN","en-US"}.Select(BD2SecretVision.Localization.Catalog.Read).ToArray();
        Check(languages[0].Keys.Order().SequenceEqual(languages[1].Keys.Order()),"language key parity");
        foreach(var entry in languages[1])Check(!string.IsNullOrWhiteSpace(entry.Value),"English content "+entry.Key);
        string preferencesRoot=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
        var prefs=new UserPreferences{Language="en-US",Settings=new(){Stages=[2,5],MaxAttempts=12,Retry=false,TargetPercent=80}};prefs.Save(preferencesRoot);
        var read=UserPreferences.Load(preferencesRoot);Check(read.Language=="en-US"&&read.Settings.Stages.SequenceEqual(new[]{2,5})&&read.Settings.TargetPercent==80&&!read.Settings.Retry&&read.Settings.MaxAttempts==12,"settings persist");
        return count;
    }
    private sealed class FakeGame:IGamePort
    {
        public GameProcess Process=new(42,1234,"synthetic.exe");public string Owner="";public int Starts,Exits;
        public List<string> Rounds=new();private readonly string root;private readonly Queue<string> outcomes;private string result="";
        private readonly Snapshot state=new(){Protocol=1,Pid=42,ProcessStart=1234,Session="fake",UIs=[new(){Type="HopscotchMainUI"}]};
        public FakeGame(string root,string[] outcomes){this.root=root;this.outcomes=new(outcomes);TestTransport.Start(root,Files.LiveEntries);WriteState();}
        private void WriteState(){state.AtUtc=DateTimeOffset.UtcNow;TestTransport.Publish(Path.Combine(root,"state.json"),state);}
        public Task<Snapshot> Read(CancellationToken token){token.ThrowIfCancellationRequested();WriteState();return Task.FromResult(state);}
        public Task<string> Send(string kind,object? fields,CancellationToken token){
            token.ThrowIfCancellationRequested();string id=Guid.NewGuid().ToString("N");
            if(kind=="start"){
                Starts++;result=outcomes.Dequeue();if(result=="unknown-command")throw new InvalidOperationException("command-result-unknown");
                state.Stage=(int)System.Text.Json.JsonSerializer.SerializeToNode(fields)!["StageIndex"]!+1;
                state.Round=id;Rounds.Add(id);state.Reason="observing";state.Board=new(){State="Playing"};state.UIs=[];
            }
            if(kind=="exit"){Exits++;state.Board=null;state.UIs=[new(){Type="HopscotchMainUI"}];}
            WriteState();return Task.FromResult(id);
        }
        public Task Play(CancellationToken token){
            token.ThrowIfCancellationRequested();state.Board!.State="Paused"; // avoid production result-wait delays in fake evidence tests
            if(result=="fail"){Files.Write(Path.Combine(root,"end.json"),new{Session=state.Session,Round=state.Round,Stage=state.Stage,AtUtc=DateTimeOffset.UtcNow,Percent=20});WriteState();return Task.CompletedTask;}
            Files.Write(Path.Combine(root,"end.json"),new{Session=state.Session,Round=result=="local-old"?"old":state.Round,Stage=state.Stage,AtUtc=DateTimeOffset.UtcNow,Percent=100,Continued=false});
            if(result!="server-missing")Files.Write(Path.Combine(root,"server-stage.json"),new{Session=state.Session,Round=result=="server-old"?"old":state.Round,AtUtc=DateTimeOffset.UtcNow,Data=new{stageId=state.Stage,isClear=true}});
            if(result=="local-old"){state.Board.State="End";state.Board.Percent=100;}
            WriteState();return Task.CompletedTask;
        }
        public Task<string> Execute(BD2SecretVision.Point start,BD2SecretVision.Point[] corners,bool drawing,CancellationToken token)=>throw new NotSupportedException();
        public void Log(string name,object value){}
    }
}
