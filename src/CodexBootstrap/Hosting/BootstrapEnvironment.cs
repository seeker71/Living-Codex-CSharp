using log4net;
using log4net.Config;

namespace CodexBootstrap.Hosting;

/// <summary>
/// Handles process-wide environment initialization prior to ASP.NET host configuration.
/// </summary>
public static class BootstrapEnvironment
{
    public static void Initialize()
    {
        LoadDotEnv();
        ConfigureLogging();
    }

    private static void LoadDotEnv()
    {
        try
        {
            var cwd = Directory.GetCurrentDirectory();
            var envFile = Path.Combine(cwd, "../../.env");
            if (!File.Exists(envFile))
            {
                Console.WriteLine($".env file not found at: {envFile}");
                return;
            }

            foreach (var line in File.ReadAllLines(envFile))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }

            Console.WriteLine($".env file loaded from: {envFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading .env file: {ex.Message}");
        }
    }

    private static void ConfigureLogging()
    {
        try
        {
            var configFile = new FileInfo("log4net.config");
            if (configFile.Exists)
            {
                XmlConfigurator.Configure(configFile);
                Console.WriteLine($"Log4net configured with config file: {configFile.FullName}");
            }
            else
            {
                Console.WriteLine($"Log4net config file not found at: {configFile.FullName}");
                BasicConfigurator.Configure();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to configure log4net: {ex.Message}");
        }
    }
}
