using System.Text.Json.Serialization;

namespace VoidgateLibrary.Models;

public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
