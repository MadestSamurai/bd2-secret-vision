using BD2SecretVision;
using BD2SecretVision.Rules;
internal static class BossWindowTests {
 public static int Run(){
  int count=0;void Check(bool value,string why){count++;if(!value)throw new Exception("Boss window: "+why);}
  double A(string state,double control,double carve)=>AttackOpportunity.Available(state,-118,-82,control,carve);
  Check(A("Move",0,0)==0,"overdue timer alone never means a safe window");
  Check(A("Move",0,30)==30,"interrupted carve overrides overdue timers");
  Check(A("Shoot",0,30)==0,"an unrelated shot is not a carve opportunity");
  Check(A("Shoot",2,0)==2,"power control survives Shoot enum");
  Check(A("Stop",0,30)==0,"unknown Stop is not assumed permanent");
  Check(A("Move",2,3)==3,"independent bounds are not added");
  Check(AttackOpportunity.Available("Move",10,5,2,0)==7,"normal controlled countdown retained");
  double C(string carve,bool interrupted,double distance=240,double speed=120,double control=0,double timer=0)=>AttackOpportunity.CarveSeconds("Move",carve,interrupted,distance,speed,control,timer);
  Check(C("Ready",true)==30,"canceled Ready coroutine remains suppressed");
  Check(C("Ready",false)==2,"active preparation has finite wall lower bound");
  Check(C("Progress",false)==2,"progress remains suppressed until wall");
  Check(C("None",true)==0,"clear state immediately removes opportunity");
  Check(C("Progress",false,0)==0,"wall contact gives no extra window");
  Check(C("Progress",false,240,240)==1,"faster boss reduces the lower bound");
  Check(C("Ready",true,timer:1)==1,"time-stop transition caps interrupted Ready");
  Check(C("Progress",false,control:2)==4,"physical control delays wall arrival");
  Check(C("Progress",false,distance:100000)==30,"bounded route horizon");
  Check(AttackOpportunity.CarveSeconds("Respawn","Ready",true,240,120,0,0)==0,"respawn invalidates carve");
  Check(AttackOpportunity.StationarySeconds("Move","Ready",true,0,0)==30,"only confirmed interrupted Ready freezes head");
  Check(AttackOpportunity.StationarySeconds("Move","Progress",false,0,0)==0,"carving body keeps moving");
  Check(AttackOpportunity.StationarySeconds("Move","Ready",false,0,0)==0,"active windup not treated as permanent freeze");
  Check(AttackOpportunity.StationarySeconds("Move","Ready",true,0,1)==1,"freeze bound honors state transition");
  Check(AttackOpportunity.DistanceToWall(5,5,0,0,4)==Math.Sqrt(2),"wall distance uses cell area rather than center");
  Check(AttackOpportunity.DistanceToWall(2,2,0,0,4)==0,"within wall has zero clearance");
  var b=new Board{State="Playing",Width=256,Height=256,CellSize=4,Player=new(){X=40,Y=1,Speed=160,Radius=30},Enemy=new(){State="Move",NormalInterval=11,NormalElapsed=129,SpecialInterval=40,SpecialElapsed=122},Rows=Enumerable.Range(0,256).Select(y=>new string(Enumerable.Range(0,256).Select(x=>x==0||y==0||x==255||y==255?'#':x<40&&y<8?'C':'.').ToArray())).ToArray()};
  var shapes=new[]{new Planning.Shape(2,[new(42,1),new(42,5),new(40,5)],12),new Planning.Shape(80,[new(120,1),new(120,5),new(40,5)],168)};
  var blocked=Planning.Choose(b,b.Player.Point,shapes,900);
  Check(!blocked.Feasible&&Planning.Window(b)==0,"reproduces logged overdue-timer stall");
  b.Enemy.CarveState="Ready";b.Enemy.CarveReadyInterrupted=true;b.Enemy.CarveBlockedRemaining=C("Ready",true);
  var choice=Planning.Choose(b,b.Player.Point,shapes,900);
  Check(choice.Feasible&&choice.Progress==80,"takes long useful route when carve is confirmed interrupted");
  Check(Math.Abs(Planning.Window(b,2)-27.45)<1e-8,"walking consumes opportunity");
  Check(Planning.Window(b,31)==0,"stale opportunity expires");
  b.Enemy.CarveBlockedRemaining=0;Check(!Planning.Choose(b,b.Player.Point,shapes,900).Feasible,"release immediately restores ordinary guard");
  b.Enemy.CarveBlockedRemaining=30;
  var pos=new[]{(40-128)*4d,(1-128)*4d};
  b.Bodies=[new(){Position=pos,Radius=30,FrozenRemaining=30}];
  Check(!Planning.Choose(b,b.Player.Point,shapes,900).Feasible,"stationary boss is still a collider");
  b.Bodies=[new(){Position=[pos[0],pos[1]+160],Radius=10,SpeedBound=500}];
  Check(!Planning.BodySafe(b,b.Player.Point,choice.Corners),"mobile body still rejects unsafe route");
  b.Bodies[0].FrozenRemaining=30;Check(Planning.BodySafe(b,b.Player.Point,choice.Corners),"confirmed frozen head no longer expands as moving");
  b.Bodies=[b.Bodies[0],new(){Position=[pos[0],pos[1]+160],Radius=10,SpeedBound=500}];
  Check(!Planning.BodySafe(b,b.Player.Point,choice.Corners),"tail is not frozen just because head is interrupted");
  return count;
 }
}
