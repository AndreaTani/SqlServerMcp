using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlServerMcp.Core.Abstractions;

namespace SqlServerMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sql")
            ?? throw new InvalidOperationException("Connection string 'Sql' is not configured.");

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<ISchemaRepository, SqlSchemaRepository>();

        return services;
    }
}
