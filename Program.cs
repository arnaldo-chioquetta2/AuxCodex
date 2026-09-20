namespace AuxCodex;

static class Program
{
    private static System.Threading.Timer? _testExitTimer;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var alternateDirectory = GetAlternateConfigurationDirectory(args);
        var activityLog = new Services.ActivityLogService();
        var configurationService = new Services.ConfigurationService(
            alternateDirectory is null ? null : new Configuration.ConfigurationPathProvider(alternateDirectory),
            activityLogService: activityLog);
        activityLog.StartNewExecution(typeof(Program).Assembly.GetName().Version?.ToString(), configurationService.ConfigurationFilePath);
        var configuration = configurationService.Load();
        activityLog.Info("Configuração carregada.");
        var context = new Services.TrayApplicationContext(configuration, configurationService, alternateDirectory is not null, activityLog);
        if (args.Any(value => string.Equals(value, "--test-exit-via-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _testExitTimer = new System.Threading.Timer(_ =>
            {
                try { context.InvokeExitMenuForTest(); if (alternateDirectory is not null) File.WriteAllText(Path.Combine(alternateDirectory, "mco14-exit-marker.txt"), "handler-complete"); }
                catch (Exception exception) { if (alternateDirectory is not null) File.WriteAllText(Path.Combine(alternateDirectory, "mco14-exit-marker.txt"), exception.GetType().Name + ":" + exception.Message); }
            }, null, 250, Timeout.Infinite);
        }
        Application.Run(context);
        _testExitTimer?.Dispose();
        _testExitTimer = null;
    }
    private static string? GetAlternateConfigurationDirectory(string[] args)
    {
        for (var index = 0; index + 1 < args.Length; index++)
            if (string.Equals(args[index], "--config-dir", StringComparison.OrdinalIgnoreCase)) return args[index + 1];
        return null;
    }
}
