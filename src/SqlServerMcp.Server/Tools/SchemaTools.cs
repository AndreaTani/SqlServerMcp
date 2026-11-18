using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlServerMcp.Core.Models;
using SqlServerMcp.Core.Services;

namespace SqlServerMcp.Server.Tools;

/// <summary>
/// MCP tools for exploring the database structure.
/// Tool classes stay thin: they describe the tool to the model and delegate to Core.
/// The descriptions are part of the product, because they are what the agent reads.
/// </summary>
[McpServerToolType]
public sealed class SchemaTools(SchemaService schemaService)
{
    [McpServerTool(Name = "list_tables", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description(
        "Lists the SQL Server tables you are allowed to query, as schema, name and an optional description. " +
        "Call this first to discover what data exists. Only these tables can be used with the other tools.")]
    public Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken cancellationToken) =>
        schemaService.ListTablesAsync(cancellationToken);

    // Next tools (see backlog):
    //   describe_table: columns, types, nullability, primary and foreign keys
    //   query_table:    structured filters turned into a parameterized SELECT TOP (n)
}
