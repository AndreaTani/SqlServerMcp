using SqlServerMcp.Core.Models;

namespace SqlServerMcp.Core.Abstractions;

/// <summary>Reads database metadata. Implemented in Infrastructure, mocked in tests.</summary>
public interface ISchemaRepository
{
    /// <summary>Returns the user tables visible to the database login.</summary>
    Task<IReadOnlyList<TableInfo>> GetTablesAsync(CancellationToken cancellationToken);
}
