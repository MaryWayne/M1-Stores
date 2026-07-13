using M1.Application.Interfaces;
using M1.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M1.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Sqlite";
        var connectionString = config.GetConnectionString("Default") ?? "Data Source=m1stores.dev.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider)
            {
                case "Postgres":
                    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
                    break;
                case "SqlServer":
                    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
                    break;
                default:
                    // Zero-setup local dev fallback. Schema comes from the same
                    // EF model that generates the PostgreSQL migrations.
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
