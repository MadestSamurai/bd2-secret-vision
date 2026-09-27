using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BD2SecretVision;

internal static class FileReliabilityTests
{
    public static async Task<int> Run()
    {
        int count=0;void Check(bool ok,string why){count++;if(!ok)throw new Exception("Files: "+why);}
        string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path=Path.Combine(root,"publication.json");
        Files.Write(path,new{Value=1});
        using(var reader=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
            Files.Write(path,new{Value=2});
            Check(Files.Read<JsonNode>(path).D("Value")==2,"replace succeeds while old reader is open");
            using var text=new StreamReader(reader,leaveOpen:true);
            Check(JsonNode.Parse(text.ReadToEnd()).D("Value")==1,"old reader sees a complete old version");
        }
        var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var release=Task.Run(async()=>{await Task.Delay(160);held.Dispose();});
        await Task.Run(()=>Files.Write(path,new{Value=3}));await release;
        Check(Files.Read<JsonNode>(path).D("Value")==3,"short lock recovers within publication retry budget");
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            var timer=Stopwatch.StartNew();LocalFileException? fault=null;
            try{Files.Write(path,new{Value=4});}catch(LocalFileException error){fault=error;}
            Check(fault!=null&&timer.Elapsed<TimeSpan.FromSeconds(3),"persistent lock bounded");
            Check(fault!.FilePath==path&&fault.Operation=="publish file"&&fault.InnerException!=null&&fault.Message.Contains("0x"),"diagnostic has path, operation, code and cause");
            Check(ErrorKind.Status(fault)=="file-access","file error never presented as game privilege error");
        }
        Check(Files.Read<JsonNode>(path).D("Value")==3,"failed replace preserves previous value");
        Check(!Directory.EnumerateFiles(root,"*.tmp").Any(),"temporary files cleaned");
        var blocker=Path.Combine(root,"not-a-folder");File.WriteAllText(blocker,"keep");
        Check(!Files.Note(Path.Combine(blocker,"events.jsonl"),new{Value=1}),"diagnostic path failure is isolated");
        Check(File.ReadAllText(blocker)=="keep","logging preserves existing paths");
        Check(ErrorKind.Status(new UnauthorizedAccessException())=="file-access","bare path denial is not process denial");
        Check(ErrorKind.Status(new Exception("open process",new System.ComponentModel.Win32Exception(5)))=="permission","actual process access denial stays distinct");

        // Repeated overlapping reads and replacements must not publish partial JSON.
        Files.Write(path,new{Value=0,Payload=new string('x',4096)});
        using(var cancel=new CancellationTokenSource()) {
            var readers=Enumerable.Range(0,2).Select(_=>Task.Run(()=>{
                int reads=0;
                while(!cancel.IsCancellationRequested) {
                    try {
                        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                        var json=JsonNode.Parse(stream)!;
                        if(json["Payload"]!.ToString()!=new string('x',4096))throw new Exception("torn publication");
                        reads++;
                    } catch(Exception error) when(Files.IsFileError(error)) {
                        // Match production readers: retain their last snapshot during
                        // transient name/handle contention. Invalid JSON is not accepted.
                    }
                }
                return reads;
            })).ToArray();
            try{for(int i=1;i<=500;i++)Files.Write(path,new{Value=i,Payload=new string('x',4096)});}
            finally{cancel.Cancel();}
            var reads=await Task.WhenAll(readers);
            Check(reads.Sum()>0&&Files.Read<JsonNode>(path).D("Value")==500,"500 atomic publications with simultaneous readers");
        }

        string leasePath=Path.Combine(root,"control.json");
        var observations=new ConcurrentQueue<string>();
        void Renew(DateTimeOffset expiry)=>Files.Write(leasePath,new{Enabled=true,ExpiresUtc=expiry});
        void Revoke()=>Files.Write(leasePath,new{Enabled=false});
        using(var heartbeat=new ControlHeartbeat(Renew,Revoke,(phase,_)=>observations.Enqueue(phase))) {
            heartbeat.Start();
            held=new FileStream(leasePath,FileMode.Open,FileAccess.Read,FileShare.Read);
            await Task.Delay(2000);held.Dispose();
            var deadline=DateTime.UtcNow.AddSeconds(3);
            while(!observations.Contains("heartbeat-recovered")&&DateTime.UtcNow<deadline)await Task.Delay(50);
            Check(observations.Contains("heartbeat-retrying")&&observations.Contains("heartbeat-recovered"),"heartbeat recovers after a lock exceeds one write retry budget");
            Check(heartbeat.Failure==null&&!heartbeat.FailureToken.IsCancellationRequested,"recovered heartbeat does not cancel run");
            Check(await heartbeat.Stop()==null&&!Files.Read<JsonNode>(leasePath).B("Enabled"),"stop joins heartbeat before revoking");
        }
        observations.Clear();
        using(var heartbeat=new ControlHeartbeat(Renew,Revoke,(phase,_)=>observations.Enqueue(phase))) {
            heartbeat.Start();held=new FileStream(leasePath,FileMode.Open,FileAccess.Read,FileShare.Read);
            try {
                using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(8));
                while(!heartbeat.FailureToken.IsCancellationRequested)await Task.Delay(50,limit.Token);
                Check(heartbeat.Failure is LocalFileException,"persistent failure cancels controller with original file cause");
                Check(observations.Contains("heartbeat-failed"),"persistent failure diagnosed");
            }finally{held.Dispose();}
            var expiry=Files.Read<JsonNode>(leasePath).S("ExpiresUtc");await Task.Delay(300);
            Check(Files.Read<JsonNode>(leasePath).S("ExpiresUtc")==expiry,"expired heartbeat never resurrects after lock release");
            await heartbeat.Stop();Check(!Files.Read<JsonNode>(leasePath).B("Enabled"),"failed heartbeat revoked");
        }
        int writes=0;
        using(var heartbeat=new ControlHeartbeat(_=>{if(++writes>1)throw new InvalidOperationException("unexpected heartbeat fault");},()=>{},(_,_)=>{})) {
            heartbeat.Start();using var limit=new CancellationTokenSource(TimeSpan.FromSeconds(3));
            while(!heartbeat.FailureToken.IsCancellationRequested)await Task.Delay(25,limit.Token);
            Check(heartbeat.Failure?.Message=="unexpected heartbeat fault","unexpected pump exceptions become visible immediately");await heartbeat.Stop();
        }
        return count;
    }
}
