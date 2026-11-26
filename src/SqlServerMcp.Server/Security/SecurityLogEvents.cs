namespace SqlServerMcp.Server.Security;

/// <summary>
/// Provides event IDs for security-related log messages.
/// <para>Numbering: 1000–1999 authentication.</para>
/// </summary>
public static class SecurityLogEvents
{
    /// <summary>
    /// A request to /mcp arrived without the API key header and was rejected.
    /// </summary>
    public const int MissingApiKeyHeader = 1001;

    /// <summary>
    /// A request to /mcp arrived with an incorrect API key and was rejected.
    /// </summary>
    public const int WrongApiKey = 1002;
}
