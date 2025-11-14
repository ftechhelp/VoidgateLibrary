namespace VoidgateLibrary;

public class VoidgateClientOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5000";
    public string? Password { get; set; }
    public TimeSpan? Timeout { get; set; }
}
