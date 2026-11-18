using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SqlServerMcp.Core.Security;

/// <summary>
/// A validated two-part table name (schema.table).
/// Identifiers are restricted to letters, digits and underscores, so a value of this
/// type can never carry SQL syntax. This is the first gate any agent input goes through.
/// </summary>
public readonly partial record struct TableName
{
    private TableName(string schema, string name)
    {
        Schema = schema;
        Name = name;
    }

    public string Schema { get; }

    public string Name { get; }

    /// <summary>Parses "Schema.Table". A missing schema defaults to "dbo".</summary>
    public static TableName Parse(string value) =>
        TryParse(value, out var result)
            ? result
            : throw new ArgumentException($"'{value}' is not a valid table name. Expected 'Schema.Table'.", nameof(value));

    public static bool TryParse([NotNullWhen(true)] string? value, out TableName result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('.');
        var (schema, name) = parts.Length switch
        {
            1 => ("dbo", parts[0]),
            2 => (parts[0], parts[1]),
            _ => (string.Empty, string.Empty),
        };

        if (!IsValidIdentifier(schema) || !IsValidIdentifier(name))
        {
            return false;
        }

        result = new TableName(schema, name);
        return true;
    }

    /// <summary>
    /// Returns the name quoted for T-SQL ([Schema].[Table]).
    /// Validation already excludes ']', but escaping it anyway costs nothing
    /// and keeps this method safe on its own.
    /// </summary>
    public string ToQuotedIdentifier() => $"[{Escape(Schema)}].[{Escape(Name)}]";

    public override string ToString() => $"{Schema}.{Name}";

    private static string Escape(string identifier) => identifier.Replace("]", "]]", StringComparison.Ordinal);

    private static bool IsValidIdentifier(string identifier) =>
        identifier.Length is > 0 and <= 128 && IdentifierRegex().IsMatch(identifier);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
