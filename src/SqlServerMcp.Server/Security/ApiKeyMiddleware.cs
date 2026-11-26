using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace SqlServerMcp.Server.Security;

/// <summary>
/// Minimal shared-secret authentication for the MCP endpoint.
/// Good enough for a demo deployment; the backlog has a story for Microsoft Entra ID (OAuth),
/// which is what the MCP authorization spec expects for real multi-user use.
/// </summary>
public sealed partial class ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeyOptions> options, ILogger<ApiKeyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyOptions.HeaderName, out var provided))
        {
            LogMissingApiKeyHeader(logger, context.Connection.RemoteIpAddress, context.Request.Headers.UserAgent);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!KeysMatch(provided.ToString(), settings.Key))
        {
            LogWrongApiKey(logger, context.Connection.RemoteIpAddress, context.Request.Headers.UserAgent);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    [LoggerMessage(
        EventId = SecurityLogEvents.MissingApiKeyHeader,
        Level = LogLevel.Information,
        Message = "Missing " + ApiKeyOptions.HeaderName + " from {RemoteIpAddress} ({UserAgent})")]
    private static partial void LogMissingApiKeyHeader(ILogger logger, IPAddress? remoteIpAddress, StringValues userAgent);

    [LoggerMessage(
        EventId = SecurityLogEvents.WrongApiKey,
        Level = LogLevel.Warning,
        Message = "Incorrect " + ApiKeyOptions.HeaderName + " from {RemoteIpAddress} ({UserAgent})")]
    private static partial void LogWrongApiKey(ILogger logger, IPAddress? remoteIpAddress, StringValues userAgent);

    // Constant-time comparison, so response timing does not reveal how much of the key was right.
    private static bool KeysMatch(string provided, string? expected)
    {
        if (string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }
}
