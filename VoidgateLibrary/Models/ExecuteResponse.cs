using System.Text.Json.Serialization;

namespace VoidgateLibrary.Models;

public class ExecuteResponse
{
    [JsonPropertyName("stdout")]
    public string Stdout { get; set; } = string.Empty;

    [JsonPropertyName("stderr")]
    public string Stderr { get; set; } = string.Empty;

    [JsonPropertyName("return_code")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
