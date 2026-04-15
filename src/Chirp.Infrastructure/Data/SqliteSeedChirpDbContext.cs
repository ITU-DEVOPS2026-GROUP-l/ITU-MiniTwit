using Microsoft.EntityFrameworkCore;

namespace Chirp.Core.Data
{
    public class SqliteSeedChirpDbContext : ChirpDbContextBase
    {
        public SqliteSeedChirpDbContext(DbContextOptions<SqliteSeedChirpDbContext> options)
            : base(options)
        {
        }
    }
}
