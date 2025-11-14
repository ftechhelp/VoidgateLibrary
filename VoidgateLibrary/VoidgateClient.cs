using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoidgateLibrary.Models;

namespace VoidgateLibrary;

public class VoidgateClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    private readonly VoidgateClientOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public VoidgateClient(HttpClient httpClient, VoidgateClientOptions? options = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsClient = false;
        _options = options ?? new VoidgateClientOptions();

        if (_http.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.BaseUrl);
        }

        if (_options.Timeout is { } t)
        {
            _http.Timeout = t;
        }
    }

    public VoidgateClient(string? baseUrl = null, string? password = null, TimeSpan? timeout = null)
    {
        _http = new HttpClient();
        _ownsClient = true;
        _options = new VoidgateClientOptions
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:5000" : baseUrl!,
            Password = password,
            Timeout = timeout
        };

        _http.BaseAddress = new Uri(_options.BaseUrl);
        if (timeout is { } t)
        {
            _http.Timeout = t;
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _http.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    // GET /health
    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/health");
        using var res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(res, cancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<HealthResponse>(body, JsonOptions);
        if (dto is null) throw new InvalidOperationException("Failed to deserialize HealthResponse");
        return dto;
    }

    // POST /execute
    public Task<ExecuteResponse> ExecuteAsync(string command, string? passwordOverride = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command is required", nameof(command));

        var request = new ExecuteRequest
        {
            Command = command,
            Password = ResolvePassword(passwordOverride)
        };
        return ExecuteAsync(request, cancellationToken);
    }

    public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Command)) throw new ArgumentException("command is required", nameof(request));
        request.Password = ResolvePassword(request.Password);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var res = await _http.PostAsync("/execute", content, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(res, cancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<ExecuteResponse>(body, JsonOptions);
        if (dto is null) throw new InvalidOperationException("Failed to deserialize ExecuteResponse");
        return dto;
    }

    // POST /run_script
    public Task<RunScriptResponse> RunScriptAsync(
        string scriptPath,
        IEnumerable<string>? args = null,
        string? workingDir = null,
        string? passwordOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new ArgumentException("scriptPath is required", nameof(scriptPath));

        var request = new RunScriptRequest
        {
            ScriptPath = scriptPath,
            Args = args?.ToList(),
            WorkingDir = workingDir,
            Password = ResolvePassword(passwordOverride)
        };
        return RunScriptAsync(request, cancellationToken);
    }

    public async Task<RunScriptResponse> RunScriptAsync(RunScriptRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.ScriptPath)) throw new ArgumentException("script_path is required", nameof(request));
        request.Password = ResolvePassword(request.Password);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var res = await _http.PostAsync("/run_script", content, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(res, cancellationToken).ConfigureAwait(false);
        var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var dto = JsonSerializer.Deserialize<RunScriptResponse>(body, JsonOptions);
        if (dto is null) throw new InvalidOperationException("Failed to deserialize RunScriptResponse");
        return dto;
    }

    private string ResolvePassword(string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate)) return candidate!;
        if (!string.IsNullOrWhiteSpace(_options.Password)) return _options.Password!;
        throw new InvalidOperationException("Password is required. Set it on the client options or pass it per-call.");
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var err = JsonSerializer.Deserialize<ErrorResponse>(body, JsonOptions);
            if (err is not null && !string.IsNullOrWhiteSpace(err.Error))
            {
                throw new VoidgateApiException(response.StatusCode, err.Error, body);
            }
        }
        catch
        {
            // Ignore parse failures; fall through to generic exception.
        }

        throw new VoidgateApiException(response.StatusCode, response.ReasonPhrase ?? "HTTP Error", body);
    }
}
