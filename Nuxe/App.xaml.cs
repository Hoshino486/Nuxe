using System.Windows;

namespace Nuxe;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Loc.Initialize(Nuxe.Properties.Settings.Default.Language);
        base.OnStartup(e);
    }
}
