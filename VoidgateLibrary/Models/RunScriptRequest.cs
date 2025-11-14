using System.Text.Json.Serialization;

namespace VoidgateLibrary.Models;

public class RunScriptRequest
{
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("script_path")]
    public string ScriptPath { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("working_dir")]
    public string? WorkingDir { get; set; }
}
