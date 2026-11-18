using System.ComponentModel.DataAnnotations;

namespace SqlServerMcp.Core.Options;

/// <summary>Security limits, bound from the "SqlMcp" configuration section.</summary>
public sealed class SqlMcpOptions
{
    public const string SectionName = "SqlMcp";

    /// <summary>Tables the agent may access, as "Schema.Table".</summary>
    [MinLength(1, ErrorMessage = "At least one table must be allowed.")]
    public List<string> AllowedTables { get; init; } = [];

    /// <summary>Default and maximum number of rows a query may return.</summary>
    [Range(1, 1000)]
    public int MaxRows { get; init; } = 100;

    /// <summary>Command timeout for every statement sent to SQL Server.</summary>
    [Range(1, 120)]
    public int CommandTimeoutSeconds { get; init; } = 15;
}
