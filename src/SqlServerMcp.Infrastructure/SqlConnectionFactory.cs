using Microsoft.Data.SqlClient;

namespace SqlServerMcp.Infrastructure;

internal interface ISqlConnectionFactory
{
    SqlConnection Create();
}

/// <summary>
/// Creates connections from the "ConnectionStrings:Sql" setting.
/// On Azure the connection string uses Managed Identity
/// (Authentication=Active Directory Default), so no password is stored anywhere.
/// </summary>
internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public SqlConnection Create()
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            // Declare read intent. On a database with readable secondaries this routes
            // the connection to a read-only replica; elsewhere it documents intent.
            // The real guarantee is the database user's permissions (see db/ scripts).
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "SqlServerMcp",
        };

        return new SqlConnection(builder.ConnectionString);
    }
}
