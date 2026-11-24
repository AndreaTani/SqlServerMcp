using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SqlServerMcp.Core.Abstractions;

namespace SqlServerMcp.Infrastructure;

public static class DependencyInjection
{
    public const string SqlConnectionStringName = "Sql";
    public const string SqlConnectionStringKey = "ConnectionStrings:" + SqlConnectionStringName;

    public static IServiceCollection AddSqlServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(SqlConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{SqlConnectionStringKey} is required.");
        }

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<ISchemaRepository, SqlSchemaRepository>();

        return services;
    }
}
