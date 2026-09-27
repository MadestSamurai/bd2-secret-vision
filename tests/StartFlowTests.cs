using BD2SecretVision;
internal static class StartFlowTests {
 public static async Task<int> Run(){
  int count=0;void Check(bool ok,string why){count++;if(!ok)throw new Exception("Start: "+why);}
  var now=DateTimeOffset.UtcNow;
  var s=new Snapshot{Round="current",AtUtc=now.AddSeconds(1),Board=new(){State="Playing"}};
  Check(AttemptRules.Ready(s,"current",now),"live Playing board starts without pause acknowledgement");
  s.Board.State="Paused";Check(!AttemptRules.Ready(s,"current",now),"paused is not ready");
  foreach(var state in new[]{"Ready","Over","End"}){s.Board.State=state;Check(!AttemptRules.Ready(s,"current",now),"non-playing start rejected: "+state);}
  s.Board.State="Playing";Check(!AttemptRules.Ready(s,"old",now),"old round rejected");Check(!AttemptRules.Ready(s,"current",now.AddSeconds(2)),"old snapshot rejected");
  foreach(var reason in new[]{"game-Paused","resume-timeout","board changed","game-Ready"})Check(AttemptRules.IsControlFault(reason),"control error never restarts rounds: "+reason);
  Check(!AttemptRules.IsControlFault("off-route"),"route failure remains retryable");
  foreach(var state in new[]{"Playing","Paused"}){
   var port=new PlannerProbe(state);
   try{await new AdaptivePlanner(port).Run(CancellationToken.None);}catch(ReachedRoute){}catch(InvalidOperationException e)when(e.Message.Contains("game-Paused")){}
   Check(port.Executed==(state=="Playing"),"planner accepts live board and respects user pause: "+state);
   Check(port.Commands==0,"planner never sends pause/resume commands: "+state);
  }
  return count;
 }
 sealed class ReachedRoute:Exception{}
 sealed class PlannerProbe:IGamePort {
  readonly Snapshot state;public bool Executed;public int Commands;
  public PlannerProbe(string gameState){
   int w=20,h=20;var rows=Enumerable.Range(0,h).Select(y=>new string(Enumerable.Range(0,w).Select(x=>x==0||y==0||x==w-1||y==h-1?'#':x>=7&&x<=12&&y>=7&&y<=12?'C':'.').ToArray())).ToArray();
   state=new(){AtUtc=DateTimeOffset.UtcNow,Board=new(){State=gameState,Width=w,Height=h,CellSize=4,Rows=rows,Player=new(){X=7,Y=7,Speed=150},Enemy=new(){State="Move",NormalInterval=9,SpecialInterval=60}}};
   TopologyTests.Edges(state.Board!);
  }
  public Task<Snapshot> Read(CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.FromResult(state);}
  public Task<string> Execute(Point start,Point[] corners,bool drawing,CancellationToken ct){Executed=true;throw new ReachedRoute();}
  public Task<string> Send(string kind,object? fields,CancellationToken ct){Commands++;throw new Exception("unexpected control command: "+kind);}
  public void Log(string name,object value){}
 }
}
