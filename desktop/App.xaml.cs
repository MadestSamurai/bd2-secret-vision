using System.IO;
using System.Windows;
using BD2SecretVision.Compatibility;
using BD2SecretVision.Localization;
namespace BD2SecretVision.Desktop;
public partial class App : Application
{
    private Mutex? mutex;
    public static string Version=>typeof(App).Assembly.GetName().Version!.ToString(3);
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try{
            if(e.Args.Length==2&&e.Args[0]=="--identity"){
                Files.Write(e.Args[1],new{version=Version,runtime="PublicRuntime4",fingerprint=HookCompiler.Fingerprint});Shutdown();return;
            }
            if(e.Args.Length==3&&e.Args[0]=="--check-client"){
                var hook=HookCompiler.Prepare(e.Args[1]);Files.Write(Path.Combine(e.Args[2],"compatibility.json"),hook.Report);
                Files.Write(Path.Combine(e.Args[2],"compile.json"),new{bytes=hook.Payload.Length,fingerprint=HookCompiler.Fingerprint,gameConnected=false});Shutdown();return;
            }
            if(e.Args.Length==2&&e.Args[0]=="--smoke") {new MainWindow(Path.GetFullPath(e.Args[1]),true).Show();return;}
            if(e.Args.Length>0){Shutdown(2);return;}
            mutex=new Mutex(true,@"Local\BD2SecretVisionAssistant",out var first);
            if(!first){var language=new Catalog(new UserPreferences().Language);MessageBox.Show(language["single-instance"],language["title"]);Shutdown();return;}
            new MainWindow(Files.Root,false).Show();
        }catch(Exception error){
            if(e.Args.Length>1){try{Files.Write(Path.Combine(e.Args[^1],"error.json"),new{error=error.ToString()});}catch{}Shutdown(1);return;}
            var language=new Catalog(new UserPreferences().Language);MessageBox.Show(language.Error(error),language["startup-error"]);Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e){mutex?.Dispose();base.OnExit(e);}
}
