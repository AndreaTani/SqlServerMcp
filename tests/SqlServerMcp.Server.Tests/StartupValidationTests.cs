using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;

using Xunit.Abstractions;

namespace SqlServerMcp.Server.Tests;

/// <summary>
/// A badly configured server should not start (e.g. invalid table in list or missing API key or API key disabled).
/// Each test breaks one setting of an otherwise valid configuration and expects startup to throw. If the server starts, the test fails.
/// </summary>
[Trait(TestTraits.Category, TestCategories.Integration)]
public sealed class StartupValidationTests : IClassFixture<StartupValidationTests.Factory>
{
    private const string TestApiKey = "test-key-123";
    private const string TestingEnvironment = "Testing";
    private readonly Factory _factory;
    private readonly ITestOutputHelper _output;

    public StartupValidationTests(Factory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public void ServerStartUp_WithInvalidTable_ThrowsException()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("SqlMcp:AllowedTables:0", "myDatabase.dbo.InvalidTable"));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Server);

        _output.WriteLine(exception.Message);

        Assert.Single(exception.Failures);
        Assert.Contains("SqlMcp:AllowedTables", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerStartUp_WithMissingApiKey_ThrowsException()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ApiKey:Key", ""));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Server);

        _output.WriteLine(exception.Message);

        Assert.Single(exception.Failures);
        Assert.Contains("ApiKey:Key", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerStartUp_WithApiKeyDisabled_ThrowsException()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ApiKey:Enabled", "false"));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Server);

        _output.WriteLine(exception.Message);

        Assert.Single(exception.Failures);
        Assert.Contains("ApiKey:Enabled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("only in Development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerStartUp_WithApiKeyDisabledInDevelopmentEnvironment_Starts()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ApiKey:Enabled", "false")
                .UseEnvironment(Environments.Development));

        var exception = Record.Exception(() => factory.Server);

        _output.WriteLine(exception?.Message ?? "No exception thrown.");

        Assert.Null(exception);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(TestingEnvironment);
            builder.UseSetting("ConnectionStrings:Sql", "Server=unused;Database=unused");
            builder.UseSetting("ApiKey:Enabled", "true");
            builder.UseSetting("ApiKey:Key", TestApiKey);
        }
    }
}
