using System.Text.Json;using System.Text.Json.Nodes;using BD2SecretVision;
int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
var fixture=JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"planning-cases.json")))!;
foreach(var row in fixture.AsArray()){
 var kind=row!.S("kind");var b=row!["board"]?.Deserialize<Board>(Files.Options);double expected=row.D("expected");
 Player P()=>row["player"]!.Deserialize<Player>(Files.Options)!;
 Point Point(JsonNode n)=>new((int)n[0]!, (int)n[1]!);
 Point[] Points(JsonNode n)=>n.AsArray().Select(x=>Point(x!)).ToArray();
 if(kind=="travel")Check(Math.Abs(Planning.Travel(P(),row.D("distance"),row.D("delay"))-expected)<1e-8,"travel");
 if(kind=="window")Check(Math.Abs(Planning.Window(b!,row.D("walk"))-expected)<1e-8,"window");
 if(kind=="body")Check(Planning.BodySafe(b!,Point(row["start"]!),Points(row["corners"]!),row.D("delay"))==row.B("expected"),"body");
 if(kind=="access"){var actual=Planning.SelectAccess(b!);Check(actual.Side==(int)row.D("side"),"side");Check(actual.U==(int)row.D("u")&&actual.V==(int)row.D("v"),"entry");Check(Math.Abs(actual.Seconds-expected)<1e-8,"entry duration");}
 if(kind=="closure")Check(Planning.Trim(b!,Point(row["start"]!),Points(row["corners"]!)).SequenceEqual(Points(row["expected"]!)),"closure");
 if(kind=="gaps"){var got=Planning.Gaps(b!);Check(got.Length==row["expected"]!.AsArray().Count,"gaps");}
}
var now=DateTimeOffset.UtcNow;JsonNode end=JsonNode.Parse($$"""{"Session":"s","Round":"r","Stage":3,"Percent":100,"Continued":false,"AtUtc":"{{now:O}}"}""")!;
JsonNode server=JsonNode.Parse($$"""{"Session":"s","Round":"r","Data":{"stageId":3,"isClear":true},"AtUtc":"{{now:O}}"}""")!;
Check(ResultRules.Confirmed(server,end,3,"s","r",now.AddSeconds(-10),100),"fresh natural end and server");
foreach(string key in new[]{"Session","Round"}){var changed=server.DeepClone();changed[key]="old";Check(!ResultRules.Confirmed(changed,end,3,"s","r",now.AddSeconds(-10),100),"reject stale "+key);}
Check(!ResultRules.Confirmed(server,end,4,"s","r",now.AddSeconds(-10),100),"wrong stage");
Check(!ResultRules.Confirmed(server,end,3,"s","r",now.AddSeconds(1),100),"old time");
var continued=end.DeepClone();continued["Continued"]=true;Check(!ResultRules.Confirmed(server,continued,3,"s","r",now.AddSeconds(-10),100),"continued cannot reach 100");
continued["Percent"]=80;Check(ResultRules.Confirmed(server,continued,3,"s","r",now.AddSeconds(-10),80),"80 target");
Check(!ResultRules.Confirmed(server,continued,3,"s","r",now.AddSeconds(-10),100),"target gap");
foreach(var invalid in new[]{new Settings{Stages=[]},new Settings{Stages=[1,1]},new Settings{Stages=[6]},new Settings{TargetPercent=90},new Settings{MaxAttempts=-1}}){bool rejected=false;try{invalid.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"settings validation");}
new Settings{Stages=[1,2,3,4,5],MaxAttempts=0}.Validate();Check(true,"all stages unlimited");
var game=new GameProcess(4,55,"game");var snapshot=new Snapshot{Protocol=1,Pid=4,ProcessStart=55,AtUtc=now,Session="s"};
Check(Connection.Fresh(snapshot,game,now),"fresh snapshot");Check(!Connection.Fresh(snapshot,game with{Start=56},now),"pid reuse");Check(!Connection.Fresh(snapshot,game,now.AddSeconds(7)),"stale heartbeat");
var strategy=await StrategyTests.Run();
var topology=await TopologyTests.Run();
var boss=BossWindowTests.Run();
var start=await StartFlowTests.Run();
var files=await FileReliabilityTests.Run();
var lifecycle=await LifecycleTests.Run();
Console.WriteLine(JsonSerializer.Serialize(new{status="pass",assertions=checks,lifecycleAssertions=lifecycle,fileReliabilityAssertions=files,startFlowAssertions=start,bossWindowAssertions=boss,topologyAssertions=topology,strategyAssertions=strategy,oracleCases=fixture.AsArray().Count,gameConnected=false}));
