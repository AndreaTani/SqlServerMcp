using System.Net.Http.Json;

namespace SqlServerMcp.Server.Tests.Support;

internal static class JsonRpcRequest
{
    private static JsonContent RpcRequest(string method, object parameters) =>
        JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters });

    internal static JsonContent ToolsList() =>
        RpcRequest("tools/list", new { });

    internal static JsonContent ToolsCall(string toolName, object? arguments = null) =>
        RpcRequest("tools/call", new { name = toolName, arguments = arguments ?? new { } });
}
