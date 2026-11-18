namespace SqlServerMcp.Server.Security;

/// <summary>Bound from the "ApiKey" configuration section.</summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    public const string HeaderName = "X-API-Key";

    /// <summary>
    /// Turn off only for local development. The server refuses to start in any
    /// other environment if this is false.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>The expected key. Store it in user-secrets locally and in Key Vault or a Container Apps secret on Azure.</summary>
    public string? Key { get; init; }
}
