using Chirp.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chirp.Core.Data
{
    public abstract class ChirpDbContextBase : IdentityDbContext<Author>
    {
        private static readonly ValueConverter<DateTime, DateTime> TimestampWithoutTimeZoneConverter =
            new(
                value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
                value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

        public DbSet<Cheep> Cheeps { get; set; } = null!;
        public DbSet<Author> Authors { get; set; } = null!;
        public DbSet<UserFollow> UserFollows { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;

        protected ChirpDbContextBase(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Author>()
                .Property(a => a.CreationDate)
                .HasConversion(TimestampWithoutTimeZoneConverter)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<Cheep>()
                .Property(c => c.TimeStamp)
                .HasConversion(TimestampWithoutTimeZoneConverter)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<UserFollow>()
                .Property(uf => uf.TimeStamp)
                .HasConversion(TimestampWithoutTimeZoneConverter)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<UserFollow>()
                .HasKey(uf => new { uf.FollowerId, uf.FolloweeId });

            modelBuilder.Entity<Like>()
                .HasKey(l => new { l.authorId, l.CheepId });

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Cheep)
                .WithMany(c => c.Likes)
                .HasForeignKey(l => l.CheepId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.Author)
                .WithMany()
                .HasForeignKey(l => l.authorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(a => a.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Followee)
                .WithMany(a => a.Followers)
                .HasForeignKey(uf => uf.FolloweeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
