using System.Text.Json.Serialization;

namespace VoidgateLibrary.Models;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
