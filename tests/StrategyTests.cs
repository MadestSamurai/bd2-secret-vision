using BD2SecretVision;
internal static class StrategyTests {
 public static async Task<int> Run(){int count=0;void Check(bool good,string why){count++;if(!good)throw new Exception("Strategy: "+why);}
  foreach(bool damaged in new[]{false,true}){
   var b=TopologyTests.Board(256,256,(x,y)=>x>=105&&x<151&&y>=105&&y<151);b.Player.X=105;b.Player.Y=112;b.Player.Speed=150;b.Enemy.NormalInterval=14;
   b.Items=[new(){Type="HopscotchItemSpeedUp",Position=[26,106],Remaining=20}];
   var direct=RepairPlanning.Choose(b,allowPickup:false)!;Check(direct.Purpose=="connect-rim"&&direct.ObjectiveProgress>=50,"useful nearby item cannot suppress strategic access generation");
   int firstRim=0,steps=0,pickups=0;bool allowPickup=true;double time=0;
   while(b.Rows.Any(r=>r.Contains('.'))&&steps<40){
    var plan=RepairPlanning.Choose(b,allowPickup:allowPickup);Check(plan!=null,"complete production-sized board has a strategic next action");
    if(plan!.Purpose=="pickup"){Check(allowPickup,"a pickup must be followed by strategic advancement");pickups++;}
    time+=plan.WalkSeconds+plan.DrawSeconds+.4;TopologyTests.Apply(b,plan);steps++;allowPickup=plan.Purpose!="pickup";
    foreach(var item in b.Items){int x=(int)Math.Floor(item.Position[0]/b.CellSize+b.Width/2d),y=(int)Math.Floor(item.Position[1]/b.CellSize+b.Height/2d);if(!item.Collected&&b.Claimed(x,y)){item.Collected=true;b.Player.Speed+=10;b.Player.SpeedStacks++;}}
    if(firstRim==0&&Planning.Boundary(b).Keys.Any(p=>RepairPlanning.RimDistance(b,p)==0))firstRim=steps;
    if(damaged&&steps==5){for(int y=1;y<12;y++){var row=b.Rows[y].ToCharArray();for(int x=90;x<96;x++)row[x]='.';b.Rows[y]=new(row);}TopologyTests.Edges(b);}
   }
   Check(firstRim>0&&firstRim<=4,"reaches rim promptly instead of central tiny captures");Check(b.Rows.All(r=>!r.Contains('.'))&&steps<=20,"full map closes after bounded strategic moves");Check(time<150,"topology-only movement budget stays below half a round");Check(pickups<=1,"nearby item does not cause repeated detours");
  }
  var controlled=TopologyTests.Board(256,256,(x,y)=>x>=105&&x<151&&y>=105&&y<151);controlled.Player.X=105;controlled.Player.Y=112;controlled.Player.Speed=150;controlled.Items=[new(){Type="HopscotchItemSpeedUp",Position=[26,106],Remaining=20}];controlled.Enemy.CarveBlockedRemaining=30;
  var opportunity=RepairPlanning.Choose(controlled)!;Check(opportunity.Purpose=="connect-rim","interrupted boss opportunity advances main route instead of detouring");Check(opportunity.ObjectiveProgress>=100,"uses long safe opportunity to reach outer boundary");
  var waiting=TopologyTests.Board(256,256,(x,y)=>x>=1&&x<4&&y>=90&&y<150);waiting.Player.X=1;waiting.Player.Y=90;waiting.Player.Speed=160;waiting.Enemy.State="Shoot";
  var fallback=RepairPlanning.Choose(waiting,allowPickup:false)!;Check(!fallback.Feasible&&fallback.DrawSeconds<.3,"no current attack window selects a short future closure rather than an impossible long route");
  var strip=TopologyTests.Board(256,256,(x,y)=>y==254&&x>=30&&x<160);strip.Player.X=160;strip.Player.Y=255;
  var transfer=Planning.SafePath(strip,new(30,255));Check(transfer.Any(p=>p.Y==254),"long transfers use inner boundary with drawing exits instead of being trapped against map wall");
  var waitProbe=new WaitProbe(waiting);await new AdaptivePlanner(waitProbe).Run(CancellationToken.None);Check(waitProbe.Keys.Count==2&&waitProbe.Keys[0]==waitProbe.Keys[1],"attack-window refusal does not blacklist the nearest objective or send player across the map");
  return count;
 }
 sealed class WaitProbe(Board board):IGamePort {
  public List<string> Keys=[];public Task<Snapshot> Read(CancellationToken ct)=>Task.FromResult(new Snapshot{Board=board,AtUtc=DateTimeOffset.UtcNow});
  public Task<string> Execute(Point start,Point[] corners,bool drawing,CancellationToken ct){Keys.Add(Topology.RouteKey(start,corners));return Task.FromResult(Keys.Count>=2?"ended":"replan-window");}
  public Task<string> Send(string kind,object? value,CancellationToken ct)=>throw new Exception("Unexpected command");public void Log(string name,object value){}
 }
}
