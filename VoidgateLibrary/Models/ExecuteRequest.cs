using System.Text.Json.Serialization;

namespace VoidgateLibrary.Models;

public class ExecuteRequest
{
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;
}
