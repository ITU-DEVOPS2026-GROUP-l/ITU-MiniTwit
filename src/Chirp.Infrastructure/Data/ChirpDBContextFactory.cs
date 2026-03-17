using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Chirp.Core.Data
{
    /// <summary>
    /// Provides a design-time factory for creating instances of
    /// <see cref="ChirpDBContext"/>.
    /// 
    /// This factory is used exclusively by Entity Framework Core tooling
    /// (such as migrations and database updates) at design time, when the
    /// application's normal dependency injection setup is not available (Because we are using Onion-Structure).
    /// 
    /// It manually configures the DbContext by loading configuration from
    /// the Chirp.Web project's appsettings.json file and supplying the
    /// required database provider and connection string.
    /// 
    /// This class is not used at runtime
    /// </summary>
    public class ChirpDBContextFactory : IDesignTimeDbContextFactory<ChirpDBContext>
    {
        public ChirpDBContext CreateDbContext(string[] args)
        {
            // Find configurationen
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Chirp.Web"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("ChirpDBConnection")
                ?? throw new InvalidOperationException("Connection string 'ChirpDBConnection' is not configured.");
            var databaseProvider = GetDatabaseProvider(configuration);

            var optionsBuilder = new DbContextOptionsBuilder<ChirpDBContext>();
            switch (databaseProvider)
            {
                case DatabaseProvider.PostgreSql:
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
                case DatabaseProvider.Sqlite:
                default:
                    optionsBuilder.UseSqlite(connectionString);
                    break;
            }

            return new ChirpDBContext(optionsBuilder.Options);
        }

        private static DatabaseProvider GetDatabaseProvider(IConfiguration configuration)
        {
            var configuredProvider = configuration["DatabaseProvider"];
            if (Enum.TryParse<DatabaseProvider>(configuredProvider, ignoreCase: true, out var provider))
            {
                return provider;
            }

            var connectionString = configuration.GetConnectionString("ChirpDBConnection");
            if (!string.IsNullOrWhiteSpace(connectionString) &&
                connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseProvider.PostgreSql;
            }

            return DatabaseProvider.Sqlite;
        }

        private enum DatabaseProvider
        {
            Sqlite,
            PostgreSql
        }
    }
}
