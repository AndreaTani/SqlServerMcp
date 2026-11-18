namespace SqlServerMcp.Core.Models;

/// <summary>A table as returned to the agent by list_tables.</summary>
/// <param name="Schema">Schema name.</param>
/// <param name="Name">Table name.</param>
/// <param name="Description">The MS_Description extended property, if any. It helps the agent pick the right table.</param>
public sealed record TableInfo(string Schema, string Name, string? Description);
