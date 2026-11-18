using Dapper;
using Microsoft.Extensions.Options;
using SqlServerMcp.Core.Abstractions;
using SqlServerMcp.Core.Models;
using SqlServerMcp.Core.Options;

namespace SqlServerMcp.Infrastructure;

internal sealed class SqlSchemaRepository(ISqlConnectionFactory connectionFactory, IOptions<SqlMcpOptions> options)
    : ISchemaRepository
{
    // Static SQL, no string concatenation. sys.tables only returns tables the
    // login has some permission on, so metadata is already limited by the database.
    private const string ListTablesSql = """
        SELECT  s.name                      AS [Schema],
                t.name                      AS [Name],
                CAST(ep.value AS nvarchar(4000)) AS [Description]
        FROM    sys.tables  AS t
        JOIN    sys.schemas AS s ON s.schema_id = t.schema_id
        LEFT JOIN sys.extended_properties AS ep
                ON  ep.major_id = t.object_id
                AND ep.minor_id = 0
                AND ep.class = 1
                AND ep.name = N'MS_Description'
        WHERE   t.is_ms_shipped = 0
        ORDER BY s.name, t.name;
        """;

    public async Task<IReadOnlyList<TableInfo>> GetTablesAsync(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();

        var command = new CommandDefinition(
            ListTablesSql,
            commandTimeout: options.Value.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TableInfo>(command).ConfigureAwait(false);
        return rows.AsList();
    }
}
