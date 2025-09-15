namespace LivingCodexMobile.Services;

public interface IApiConfiguration
{
    string BaseUrl { get; set; }
    TimeSpan Timeout { get; set; }
    bool EnableLogging { get; set; }
    bool EnableRetry { get; set; }
    int MaxRetryAttempts { get; set; }
}

public class ApiConfiguration : IApiConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:5002";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableLogging { get; set; } = true;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
}
