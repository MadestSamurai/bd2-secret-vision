using System.Text.Json;using BD2SecretVision.Compatibility;
try {
 if(args.Length==3 && args[0]=="contract"){using var index=new MetadataIndex(Path.Combine(args[1],"Assembly-CSharp.dll"));var c=ContractGenerator.Generate(index,args[2]);File.WriteAllText(Path.Combine(args[2],"compatibility","contract.json"),JsonSerializer.Serialize(c,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Contract: {c.Types.Length} types / {c.Types.Sum(t=>t.Members.Length)} members");}
 else if(args.Length>=2&&args[0]=="check"){var p=HookCompiler.Prepare(args[1]);Console.WriteLine(JsonSerializer.Serialize(p.Report));Console.WriteLine($"Compiled {p.Payload.Length} bytes without connecting to game.");}
 else throw new ArgumentException("contract <Managed> <repo> | check <Managed>");
}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
