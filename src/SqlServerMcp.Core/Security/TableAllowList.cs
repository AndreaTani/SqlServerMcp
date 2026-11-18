namespace SqlServerMcp.Core.Security;

/// <summary>
/// The only tables an agent may see or query. Anything not listed here does not exist
/// as far as the agent is concerned, even if the database user could read it.
/// Matching is case-insensitive, like SQL Server's default collation.
/// </summary>
public sealed class TableAllowList
{
    private readonly HashSet<string> _keys;

    public TableAllowList(IEnumerable<string> tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);

        Tables = tableNames
            .Select(TableName.Parse)
            .DistinctBy(Key, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        _keys = new HashSet<string>(Tables.Select(Key), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TableName> Tables { get; }

    public bool IsAllowed(TableName table) => _keys.Contains(Key(table));

    public bool IsAllowed(string schema, string name) =>
        TableName.TryParse($"{schema}.{name}", out var table) && IsAllowed(table);

    private static string Key(TableName table) => table.ToString();
}
