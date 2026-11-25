using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using SqlServerMcp.Core.Abstractions;
using SqlServerMcp.Core.Models;
using SqlServerMcp.Server.Tests.Support;

namespace SqlServerMcp.Server.Tests;

/// <summary>
/// Hosts the real ASP.NET Core pipeline in memory, with the database replaced by a mock.
/// Verifies the HTTP-level security: health is public, /mcp requires the API key.
/// </summary>
[Trait(TestTraits.Category, TestCategories.Integration)]
public sealed class McpEndpointTests : IClassFixture<McpEndpointTests.Factory>
{
    private const string TestApiKey = "test-key-123";
    private const string WrongApiKey = "wrong-key-7f3a9c";
    private readonly Factory _factory;

    public McpEndpointTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Health_WithoutApiKey_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_WithoutApiKey_ReturnsUnauthorized()
    {
        using var client = _factory.CreateMcpClient(apiKey: null);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_WithWrongApiKey_ReturnsUnauthorized()
    {
        using var client = _factory.CreateMcpClient(WrongApiKey);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_WithApiKey_ListsTheListTablesTool()
    {
        using var client = _factory.CreateMcpClient(TestApiKey);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsList());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("list_tables", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mcp_CallListTables_ReturnsOnlyAllowedTables()
    {
        using var client = _factory.CreateMcpClient(TestApiKey);

        var response = await client.PostAsync(new Uri("/mcp", UriKind.Relative), JsonRpcRequest.ToolsCall("list_tables"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Product", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Customer", body, StringComparison.Ordinal);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Sql", "Server=unused;Database=unused");
            builder.UseSetting("ApiKey:Enabled", "true");
            builder.UseSetting("ApiKey:Key", TestApiKey);

            builder.ConfigureTestServices(services =>
            {
                IReadOnlyList<TableInfo> tables = 
                [
                    new TableInfo("SalesLT", "Product", null),
                    new TableInfo("SalesLT", "Customer", null),
                ];
                var repository = new Mock<ISchemaRepository>();
                repository
                    .Setup(r => r.GetTablesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tables);

                services.AddScoped(_ => repository.Object);
            });
        }
    }
}
