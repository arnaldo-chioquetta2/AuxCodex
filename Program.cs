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
        var pathProvider = alternateDirectory is null
            ? new Configuration.ConfigurationPathProvider()
            : new Configuration.ConfigurationPathProvider(alternateDirectory);
        new Services.LegacyConfigurationMigrationService().MigrateIfNeeded(pathProvider, alternateDirectory is not null, activityLog);
        var configurationService = new Services.ConfigurationService(pathProvider, activityLogService: activityLog);
        activityLog.StartNewExecution(typeof(Program).Assembly.GetName().Version?.ToString(), configurationService.ConfigurationFilePath, alternateDirectory is not null);
        var configuration = configurationService.Load();
        activityLog.ConfigurationLoaded(configuration);
        var context = new Services.TrayApplicationContext(configuration, configurationService, alternateDirectory is not null, activityLog);
        if (args.Any(value => string.Equals(value, "--test-exit-via-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _testExitTimer = new System.Threading.Timer(_ =>
            {
                try { context.InvokeExitMenuForTest(); if (alternateDirectory is not null) File.WriteAllText(Path.Combine(alternateDirectory, "mco14-exit-marker.txt"), "handler-complete"); }
                catch (Exception exception) { if (alternateDirectory is not null) File.WriteAllText(Path.Combine(alternateDirectory, "mco14-exit-marker.txt"), exception.GetType().Name + ":" + exception.Message); }
            }, null, 250, Timeout.Infinite);
        }
        Forms.SplashForm? splash = null;
        try
        {
            try
            {
                splash = new Forms.SplashForm();
                splash.Show();
            }
            catch (Exception exception)
            {
                activityLog.Warning($"Splash não pôde ser exibida; inicialização continuará: {exception.GetType().Name}.");
            }

            Application.Run(context);
        }
        finally
        {
            if (splash is not null && !splash.IsDisposed)
            {
                try { splash.Close(); }
                catch (ObjectDisposedException) { }
            }
            _testExitTimer?.Dispose();
            _testExitTimer = null;
        }
    }
    private static string? GetAlternateConfigurationDirectory(string[] args)
    {
        for (var index = 0; index + 1 < args.Length; index++)
            if (string.Equals(args[index], "--config-dir", StringComparison.OrdinalIgnoreCase)) return args[index + 1];
        return null;
    }
}
