using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using SqlServerMcp.Infrastructure;
using SqlServerMcp.Server.Security;
using SqlServerMcp.Server.Tests.Support;

namespace SqlServerMcp.Server.Tests;

[Trait(TestTraits.Category, TestCategories.Integration)]
public sealed class ApiKeyLoggingTests : IClassFixture<ApiKeyLoggingTests.Factory>
{
    private const string TestApiKey = "test-key-123";
    private const string WrongApiKey = "wrong-key-7f3a9c";
    private const string TestingEnvironment = "Testing";

    private readonly Factory _factory;
    private readonly FakeLogCollector _logs;

    public ApiKeyLoggingTests(Factory factory)
    {
        _factory = factory;
        _logs = _factory.Services.GetFakeLogCollector();
        _logs.Clear();
    }

    [Fact]
    public async Task Mcp_WithoutApiKey_LogsMissingHeaderAtInformation()
    {
        using var client = _factory.CreateMcpClient(apiKey: null);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());
        var missingHeaderRecords = _logs.GetSnapshot().Where(l => l.Id == SecurityLogEvents.MissingApiKeyHeader);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var record = Assert.Single(missingHeaderRecords);
        Assert.Equal(LogLevel.Information, record.Level);
    }

    [Fact]
    public async Task Mcp_WithWrongApiKey_LogsWrongKeyAtWarning()
    {
        using var client = _factory.CreateMcpClient(apiKey: WrongApiKey);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());
        var wrongApiKeyRecords = _logs.GetSnapshot().Where(l => l.Id == SecurityLogEvents.WrongApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var record = Assert.Single(wrongApiKeyRecords);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.DoesNotContain(WrongApiKey, record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mcp_WithCorrectApiKey_DoesNotLogSecurityEvents()
    {
        using var client = _factory.CreateMcpClient(apiKey: TestApiKey);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());
        var logRecords = _logs.GetSnapshot();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(logRecords, l => l.Id == SecurityLogEvents.MissingApiKeyHeader || l.Id == SecurityLogEvents.WrongApiKey);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(TestingEnvironment);
            builder.UseSetting(DependencyInjection.SqlConnectionStringKey, "Server=unused;Database=unused");
            builder.UseSetting("ApiKey:Enabled", "true");
            builder.UseSetting("ApiKey:Key", TestApiKey);

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddFakeLogging();
            });
        }
    }
}
