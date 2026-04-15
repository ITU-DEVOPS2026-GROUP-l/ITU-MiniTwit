using Microsoft.EntityFrameworkCore;

namespace Chirp.Core.Data
{
    public class ChirpDBContext : ChirpDbContextBase
    {
        public ChirpDBContext(DbContextOptions<ChirpDBContext> options)
            : base(options)
        {
        }
    }
}
