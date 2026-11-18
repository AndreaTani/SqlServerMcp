using SqlServerMcp.Core.Abstractions;
using SqlServerMcp.Core.Models;
using SqlServerMcp.Core.Security;

namespace SqlServerMcp.Core.Services;

/// <summary>
/// Applies the allow-list to what the repository returns.
/// The database permissions are the first line of defence; this is the second.
/// If someone grants the login more than intended, the agent still sees only the allowed tables.
/// </summary>
public sealed class SchemaService(ISchemaRepository repository, TableAllowList allowList)
{
    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken cancellationToken)
    {
        var tables = await repository.GetTablesAsync(cancellationToken).ConfigureAwait(false);

        return tables
            .Where(t => allowList.IsAllowed(t.Schema, t.Name))
            .OrderBy(t => t.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
