using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Chirp.Core.Data
{
    public class ChirpDBContextFactory : IDesignTimeDbContextFactory<ChirpDBContext>
    {
        public ChirpDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Chirp.Web"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("ChirpPrimaryConnection");

            var optionsBuilder = new DbContextOptionsBuilder<ChirpDBContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ChirpDBContext(optionsBuilder.Options);
        }
    }
}
