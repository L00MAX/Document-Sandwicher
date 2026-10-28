using System.Windows;

namespace DocumentSandwich;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0 && e.Args[0] == "--self-test")
        {
            try { SelfTest.Run(e.Args[1]); Shutdown(0); }
            catch (Exception ex) { System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[1], "failure.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        new MainWindow().Show();
    }
}
