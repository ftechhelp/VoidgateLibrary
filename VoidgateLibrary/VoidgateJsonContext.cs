using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VoidgateLibrary;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    WriteIndented = false
)]
[JsonSerializable(typeof(Models.ExecuteRequest))]
[JsonSerializable(typeof(Models.ExecuteResponse))]
[JsonSerializable(typeof(Models.RunScriptRequest))]
[JsonSerializable(typeof(Models.RunScriptResponse))]
[JsonSerializable(typeof(Models.HealthResponse))]
[JsonSerializable(typeof(Models.ErrorResponse))]
[JsonSerializable(typeof(List<string>))]
internal partial class VoidgateJsonContext : JsonSerializerContext
{
}
