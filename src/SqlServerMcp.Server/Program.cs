using Microsoft.Extensions.Options;
using SqlServerMcp.Core.Options;
using SqlServerMcp.Core.Security;
using SqlServerMcp.Core.Services;
using SqlServerMcp.Infrastructure;
using SqlServerMcp.Server.Security;

var builder = WebApplication.CreateBuilder(args);

// Configuration, validated at startup: a misconfigured server should not start at all.
builder.Services
    .AddOptions<SqlMcpOptions>()
    .Bind(builder.Configuration.GetSection(SqlMcpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Key), "ApiKey:Key is required when ApiKey:Enabled is true.")
    .Validate(o => o.Enabled || builder.Environment.IsDevelopment(), "ApiKey:Enabled can be false only in Development.")
    .ValidateOnStart();

// Core and infrastructure.
builder.Services.AddSingleton(sp =>
    new TableAllowList(sp.GetRequiredService<IOptions<SqlMcpOptions>>().Value.AllowedTables));
builder.Services.AddScoped<SchemaService>();
builder.Services.AddSqlServerInfrastructure(builder.Configuration);

// MCP over Streamable HTTP. SDK v2 is stateless by default, which suits
// Azure Container Apps: any replica can serve any request, no sticky sessions.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Liveness probe for Container Apps. Deliberately outside the API key check.
app.MapHealthChecks("/health");

// Everything under /mcp requires the API key.
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/mcp"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.MapMcp("/mcp");

await app.RunAsync();

// No "public partial class Program" needed: since .NET 10 a source generator makes
// Program public, so WebApplicationFactory<Program> works out of the box.
