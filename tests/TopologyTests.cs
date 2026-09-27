using BD2SecretVision;
using BD2SecretVision.Rules;
using System.Diagnostics;
using System.Text.Json;
internal static class TopologyTests {
 public static Board Board(int w=24,int h=24,Func<int,int,bool>? claimed=null){
  claimed??=(x,y)=>x>=8&&x<=14&&y>=8&&y<=14;
  var b=new Board{Id=1,State="Playing",Width=w,Height=h,CellSize=4,Player=new(){X=8,Y=8,Speed=180,Radius=1},Enemy=new(){State="Move",NormalInterval=60,SpecialInterval=120},Rows=Enumerable.Range(0,h).Select(y=>new string(Enumerable.Range(0,w).Select(x=>x==0||y==0||x==w-1||y==h-1?'#':claimed(x,y)?'C':'.').ToArray())).ToArray()};Edges(b);return b;
 }
 public static void Edges(Board b,bool retain=false){
  var oldH=b.HorizontalEdges;var oldV=b.VerticalEdges;
  b.HorizontalEdges=Enumerable.Range(0,b.Height+1).Select(y=>new string(Enumerable.Range(0,b.Width).Select(x=>y==0||y==b.Height?'#':b.Claimed(x,y)!=b.Claimed(x,y-1)?'C':retain&&oldH[y][x]=='C'&&!b.Claimed(x,y)&&!b.Claimed(x,y-1)?'C':'.').ToArray())).ToArray();
  b.VerticalEdges=Enumerable.Range(0,b.Height).Select(y=>new string(Enumerable.Range(0,b.Width+1).Select(x=>x==0||x==b.Width?'#':b.Claimed(x,y)!=b.Claimed(x-1,y)?'C':retain&&oldV[y][x]=='C'&&!b.Claimed(x,y)&&!b.Claimed(x-1,y)?'C':'.').ToArray())).ToArray();Key(b);
 }
 static void Key(Board b)=>b.TopologyKey=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('|',b.Rows.Concat(b.HorizontalEdges).Concat(b.VerticalEdges)))));
 public static void Promote(Board b,Point start,Point[] corners){var a=start;foreach(var z in Topology.Expand(start,corners).Skip(1)){bool h=a.Y==z.Y;var rows=h?b.HorizontalEdges:b.VerticalEdges;int y=h?a.Y:Math.Min(a.Y,z.Y),x=h?Math.Min(a.X,z.X):a.X;var chars=rows[y].ToCharArray();chars[x]='C';rows[y]=new(chars);a=z;}Key(b);}
 public static void Apply(Board b,RepairPlanning.Plan plan){var fill=new Topology(b).Predict(plan.Start,plan.Corners,true);Promote(b,plan.Start,plan.Corners);foreach(var group in fill.Filled.GroupBy(i=>i/b.Width)){var row=b.Rows[group.Key].ToCharArray();foreach(int index in group)row[index%b.Width]='C';b.Rows[group.Key]=new(row);}Edges(b,true);b.Player.X=plan.Corners[^1].X;b.Player.Y=plan.Corners[^1].Y;}
 public static async Task<int> Run(){int count=0;void Check(bool good,string why){count++;if(!good)throw new Exception("Topology: "+why);}
  var b=Board(24,24,(x,y)=>x>=1&&x<6&&y>=1&&y<9);b.Player.X=6;b.Player.Y=1;
  Point[] u=[new(10,1),new(10,5),new(6,5)];var trim=Planning.Trim(b,b.Player.Point,u);var before=string.Join('|',b.HorizontalEdges.Concat(b.VerticalEdges));var prediction=new Topology(b).Predict(b.Player.Point,trim,true);
  Check(prediction.Cells==16&&prediction.RimCells==4,"U claim matches native outside flood");Check(prediction.Filled.Length==16,"claimed cells available for item attribution");Check(before==string.Join('|',b.HorizontalEdges.Concat(b.VerticalEdges)),"prediction never mutates board");
  Check(Topology.Edge(b,new(12,1),new(13,1))=='.',"inner wall row is not automatically a safe edge");
  var plan=RepairPlanning.Choose(b)!;Check(plan.Cells>0,"rim extension makes real progress");
  var orphan=Board(24,24,(x,y)=>x>=3&&x<7&&y>=8&&y<12);orphan.Player.X=7;orphan.Player.Y=8;Promote(orphan,new(7,8),[new(12,8),new(12,3),new(17,3),new(17,8)]);
  Check(!orphan.Claimed(12,3)&&Topology.Safe(orphan,new(12,3),new(13,3)),"promoted edge retained with empty cells on both sides");
  Check(Planning.Boundary(orphan).ContainsKey(new(17,3)),"reachable orphan edge is preserved");Check(new Topology(orphan).Predict(new(12,3),[new(17,3)]).NewEdges==0,"old line is not new progress");
  bool rejected=false;try{Planning.Trim(orphan,new(12,3),[new(17,3)]);}catch(RouteFailure){rejected=true;}Check(rejected,"safe-only oscillation is not a drawing route");
  Check(Topology.RouteKey(new(12,3),[new(17,3)])==Topology.RouteKey(new(17,3),[new(12,3)]),"reverse oscillation uses same exclusion key");
  var fixedPlan=RepairPlanning.Choose(orphan);Check(fixedPlan!=null&&fixedPlan.NewEdges>0,"damaged component chooses a new connector");
  // The last gap can close through pre-existing safe edges, despite zero polygon shoelace area.
  var rim=Board(20,16,(x,y)=>y==1||y==14||x==1||x==18);var cells=rim.Rows[1].ToCharArray();cells[7]='.';rim.Rows[1]=new(cells);Edges(rim);rim.Player.X=7;rim.Player.Y=1;
  var straight=Planning.Trim(rim,new(7,1),[new(8,1)]);var finish=new Topology(rim).Predict(new(7,1),straight);Check(finish.Cells>150,"one straight final edge seals outside connectivity");Check(finish.RimCells==1,"final small gap yields whole interior");
  var shortPlan=RepairPlanning.Choose(rim);Check(shortPlan!=null&&shortPlan.Cells>150,"planner finds the high-gain straight repair");
  var early=Planning.Trim(rim,new(7,1),[new(8,1),new(8,8),new(7,8),new(7,1)]);Check(early.SequenceEqual(new[]{new Point(8,1)}),"stops at first native closure, not a fabricated polygon");
  var malformed=Board();malformed.HorizontalEdges[3]=".";rejected=false;try{new Topology(malformed);}catch(RouteFailure){rejected=true;}Check(rejected,"incomplete edge data rejected before indexing");
  foreach(var dims in new[]{(24,24),(32,18),(18,32)}){
   var state=Board(dims.Item1,dims.Item2,(x,y)=>x>=6&&x<11&&y>=6&&y<11);state.Player.X=6;state.Player.Y=6;int steps=0;
   while(state.Rows.Any(r=>r.Contains('.'))&&steps++<80){var next=RepairPlanning.Choose(state);Check(next!=null,"reachable current graph always produces progress");Apply(state,next!);}
   Check(state.Rows.All(r=>!r.Contains('.')),"fresh board closes on rectangular and non-square maps");
  }
  foreach(var start in new[]{new Point(4,1),new Point(1,4),new Point(4,15),new Point(19,4)}){
   Point P(BorderEscapeRules.Vertex p)=>new(p.X,p.Y);
   bool Drawable(BorderEscapeRules.Vertex a,BorderEscapeRules.Vertex z){var (x1,y1,x2,y2)=Topology.Adjacent(P(a),P(z));return rim.Rows[y1][x1]=='.'||rim.Rows[y2][x2]=='.';}
   var routes=BorderEscapeRules.DrawingRoutes(new(start.X,start.Y),rim.Width,rim.Height,Drawable,(a,z)=>Topology.Safe(rim,P(a),P(z)),v=>Topology.Border(rim,P(v))).ToArray();
   // A fully claimed rim has no legal draw to the map wall; it must go to the empty inner side.
   Check(routes.Length==0,"never draw outward into impassable map wall or claimed area");
  }
  foreach(var start in new[]{new Point(6,3),new Point(6,7)}){
   Point P(BorderEscapeRules.Vertex p)=>new(p.X,p.Y);
   bool Drawable(BorderEscapeRules.Vertex a,BorderEscapeRules.Vertex z){var (x1,y1,x2,y2)=Topology.Adjacent(P(a),P(z));return b.Rows[y1][x1]=='.'||b.Rows[y2][x2]=='.';}
   var routes=BorderEscapeRules.DrawingRoutes(new(start.X,start.Y),b.Width,b.Height,Drawable,(a,z)=>Topology.Safe(b,P(a),P(z)),v=>Topology.Border(b,P(v))).ToArray();
   Check(routes.Length>0,"emergency routes exist from exposed boundary");
   Check(routes.All(r=>!Topology.Safe(b,start,P(r[0]))),"every emergency route leaves safe boundary on its first step");
   Check(routes.All(r=>Topology.Border(b,P(r[^1]))),"every emergency route returns to an existing boundary");
   Check(routes.All(r=>r.Take(r.Length-1).All(v=>!Topology.Border(b,P(v)))),"no emergency route continues past early closure");
  }
  Check(BorderEscapeRules.TimingAllows(0,.7,true),"imminent lightning escapes without waiting for attack window");
  Check(!BorderEscapeRules.TimingAllows(0,.7,false),"normal departure retains attack timing");
  Check(!BorderEscapeRules.TimingAllows(0,2,true),"lightning does not justify a long exposed loop");
  Check(BorderEscapeRules.TimingAllows(5,2,false),"normal sufficient window unchanged");
  Check(BorderEscapeRules.SweptDistance(-40,0,40,0)==0,"fast lightning crossing between samples is detected");
  Check(BorderEscapeRules.SweptDistance(-40,20,40,20)==20,"parallel harmless pass remains clear");
  Check(BorderEscapeRules.SweptDistance(5,0,9,0)==5,"past crossing not extrapolated backward");
  Check(BorderEscapeRules.SweptDistance(3,4,3,4)==5,"stationary lightning handled");
  var port=new NoGainPort(b);await new AdaptivePlanner(port).Run(CancellationToken.None);Check(port.Keys.Count==3&&port.Keys.Distinct().Count()==3,"completed no-gain route is excluded instead of oscillating");
  // Production-size timing is measured after warmup. It is not a strict shared-machine wall-clock assertion.
  var large=Board(256,256,(x,y)=>x>=118&&x<138&&y>=118&&y<138);large.Player.X=118;large.Player.Y=118;RepairPlanning.Choose(large);var sw=Stopwatch.StartNew();var largePlan=RepairPlanning.Choose(large);sw.Stop();Check(largePlan!=null&&largePlan.Cells>0,"full-size initial access predicts real fill");Console.WriteLine(JsonSerializer.Serialize(new{topologyDecisionMilliseconds=sw.Elapsed.TotalMilliseconds,cells=largePlan!.Cells,gameConnected=false}));
  return count;
 }
 sealed class NoGainPort(Board board):IGamePort {
  public List<string> Keys=[];public Task<Snapshot> Read(CancellationToken ct)=>Task.FromResult(new Snapshot{Board=board,AtUtc=DateTimeOffset.UtcNow});
  public Task<string> Execute(Point start,Point[] corners,bool drawing,CancellationToken ct){Keys.Add(Topology.RouteKey(start,corners));return Task.FromResult(Keys.Count>=3?"ended":"completed");}
  public Task<string> Send(string kind,object? value,CancellationToken ct)=>throw new Exception("Unexpected command");public void Log(string name,object value){}
 }
}
