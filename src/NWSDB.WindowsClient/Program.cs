namespace NWSDB.WindowsClient;

internal static class Program
{
    /// <summary>NWSDB desktop client for customer service counters and billing staff.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
