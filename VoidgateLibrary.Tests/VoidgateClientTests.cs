using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;
using VoidgateLibrary.Models;
using VoidgateLibrary.Tests.Http;
using Xunit;

namespace VoidgateLibrary.Tests;

public class VoidgateClientTests
{
    private static HttpClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:5000")
        };
        return http;
    }

    [Fact]
    public async Task ExecuteAsync_SendsPasswordAndCommand_InSnakeCase()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new ExecuteResponse
                {
                    Stdout = "ok",
                    Stderr = "",
                    ReturnCode = 0,
                    Success = true
                }), Encoding.UTF8, "application/json")
            })
        };
        var client = new VoidgateClient(CreateClient(handler), new VoidgateClientOptions { Password = "pwd" });

        var result = await client.ExecuteAsync("ls -la");

        result.Success.Should().BeTrue();
        handler.LastRequest.Should().NotBeNull();
        var body = handler.LastRequestBody;
        body.Should().Contain("\"password\":\"pwd\"");
        body.Should().Contain("\"command\":\"ls -la\"");
    }

    [Fact]
    public async Task ExecuteAsync_200WithSuccessFalse_ReturnsResultWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{" +
                    "\"stdout\":\"\",\"stderr\":\"error\",\"return_code\":2,\"success\":false}" , Encoding.UTF8, "application/json")
            })
        };
        var client = new VoidgateClient(CreateClient(handler), new VoidgateClientOptions { Password = "pwd" });

        var result = await client.ExecuteAsync("badcmd");

        result.Success.Should().BeFalse();
        result.Stderr.Should().Be("error");
        result.ReturnCode.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_401WithError_ThrowsVoidgateApiException()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"Invalid password\"}", Encoding.UTF8, "application/json")
            })
        };
        var client = new VoidgateClient(CreateClient(handler), new VoidgateClientOptions { Password = "bad" });

        var act = async () => await client.ExecuteAsync("ls");
        var ex = await Assert.ThrowsAsync<VoidgateApiException>(act);
        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.ResponseBody.Should().Contain("Invalid password");
    }

    [Fact]
    public async Task GetHealthAsync_ParsesHealthy()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"healthy\"}", Encoding.UTF8, "application/json")
            })
        };
        var client = new VoidgateClient(CreateClient(handler));

        var health = await client.GetHealthAsync();
        health.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task RunScriptAsync_SendsSnakeCaseFields()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new RunScriptResponse
                {
                    Stdout = "ran",
                    Stderr = "",
                    ReturnCode = 0,
                    Success = true,
                    ScriptPath = "/abs/script.sh",
                    Args = new List<string> { "--flag", "value" },
                    WorkingDir = "/abs/dir"
                }), Encoding.UTF8, "application/json")
            })
        };
        var client = new VoidgateClient(CreateClient(handler), new VoidgateClientOptions { Password = "pwd" });

        var res = await client.RunScriptAsync("/abs/script.sh", new[] { "--flag", "value" }, "/abs/dir");

        res.Success.Should().BeTrue();
        var reqJson = handler.LastRequestBody!;
        reqJson.Should().Contain("\"script_path\":\"/abs/script.sh\"");
        reqJson.Should().Contain("\"args\":[\"--flag\",\"value\"]");
        reqJson.Should().Contain("\"working_dir\":\"/abs/dir\"");
        reqJson.Should().Contain("\"password\":\"pwd\"");
    }
}
