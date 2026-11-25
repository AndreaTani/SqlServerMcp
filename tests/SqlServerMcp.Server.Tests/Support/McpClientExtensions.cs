using Microsoft.AspNetCore.Mvc.Testing;

using SqlServerMcp.Server.Security;

namespace SqlServerMcp.Server.Tests.Support;

internal static class McpClientExtensions
{
    public static HttpClient CreateMcpClient(this WebApplicationFactory<Program> factory, string? apiKey)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json, text/event-stream");

        if (apiKey is not null)
        {
            client.DefaultRequestHeaders.Add(ApiKeyOptions.HeaderName, apiKey);
        }

        return client;
    }
}