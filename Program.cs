using IPScan.UI;

namespace IPScan;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// Does not initiate any scans on startup.
    /// </summary>
    [STAThread]
    static void Main()
    {
        AppIconHelper.RegisterAppUserModelId();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}
