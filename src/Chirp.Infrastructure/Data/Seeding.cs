using Chirp.Core.Data;
using Chirp.Core.Models;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chirp.Infrastructure.Data
{
    public class Seeding
    {
        private readonly ChirpDBContext _targetContext;
        private readonly SqliteSeedChirpDbContext _sourceContext;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<Seeding> _logger;

        public Seeding(
            ChirpDBContext targetContext,
            SqliteSeedChirpDbContext sourceContext,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment,
            ILogger<Seeding> logger)
        {
            _targetContext = targetContext;
            _sourceContext = sourceContext;
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task EnsureMigratedAndSeededAsync(CancellationToken cancellationToken = default)
        {
            await _targetContext.Database.MigrateAsync(cancellationToken);

            var sqlitePath = TryResolveLegacySqlitePath();
            _logger.LogInformation(
                "PostgreSQL startup seed check. Legacy SQLite source: {LegacySqlitePath}",
                sqlitePath ?? "unresolved");

            var tableState = await GetTargetTableStateAsync(cancellationToken);
            if (tableState.IsFullySeeded)
            {
                _logger.LogInformation(
                    "Skipping SQLite-to-PostgreSQL seed import because target database is already seeded. Authors: {Authors}, Cheeps: {Cheeps}, Follows: {Follows}, Likes: {Likes}, Roles: {Roles}, RoleClaims: {RoleClaims}, UserClaims: {UserClaims}, UserLogins: {UserLogins}, UserRoles: {UserRoles}, UserTokens: {UserTokens}",
                    tableState.Authors,
                    tableState.Cheeps,
                    tableState.Follows,
                    tableState.Likes,
                    tableState.Roles,
                    tableState.RoleClaims,
                    tableState.UserClaims,
                    tableState.UserLogins,
                    tableState.UserRoles,
                    tableState.UserTokens);
                return;
            }

            _logger.LogInformation(
                "Target PostgreSQL seed state before import. Authors: {Authors}, Cheeps: {Cheeps}, Follows: {Follows}, Likes: {Likes}, Roles: {Roles}, RoleClaims: {RoleClaims}, UserClaims: {UserClaims}, UserLogins: {UserLogins}, UserRoles: {UserRoles}, UserTokens: {UserTokens}",
                tableState.Authors,
                tableState.Cheeps,
                tableState.Follows,
                tableState.Likes,
                tableState.Roles,
                tableState.RoleClaims,
                tableState.UserClaims,
                tableState.UserLogins,
                tableState.UserRoles,
                tableState.UserTokens);

            if (!LegacySqliteDatabaseExists())
            {
                _logger.LogWarning(
                    "Skipping SQLite-to-PostgreSQL seed import because the legacy SQLite database was not found at {LegacySqlitePath}",
                    sqlitePath ?? "unresolved");
                return;
            }

            var identityCounts = await CopyIdentityDataAsync(tableState, cancellationToken);
            var domainCounts = await CopyDomainDataAsync(tableState, cancellationToken);
            await ResetIdentitySequencesAsync(cancellationToken);

            _logger.LogInformation(
                "SQLite-to-PostgreSQL seed import completed. Imported Authors: {Authors}, Cheeps: {Cheeps}, Follows: {Follows}, Likes: {Likes}, Roles: {Roles}, RoleClaims: {RoleClaims}, UserClaims: {UserClaims}, UserLogins: {UserLogins}, UserRoles: {UserRoles}, UserTokens: {UserTokens}",
                identityCounts.Authors,
                domainCounts.Cheeps,
                domainCounts.Follows,
                domainCounts.Likes,
                identityCounts.Roles,
                identityCounts.RoleClaims,
                identityCounts.UserClaims,
                identityCounts.UserLogins,
                identityCounts.UserRoles,
                identityCounts.UserTokens);
        }

        private bool LegacySqliteDatabaseExists()
        {
            var connectionString = _configuration.GetConnectionString("LegacySqliteConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource))
            {
                return false;
            }

            if (builder.DataSource == ":memory:")
            {
                return true;
            }

            var fullPath = Path.IsPathRooted(builder.DataSource)
                ? builder.DataSource
                : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, builder.DataSource));

            return File.Exists(fullPath);
        }

        private string? TryResolveLegacySqlitePath()
        {
            var connectionString = _configuration.GetConnectionString("LegacySqliteConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
            {
                return builder.DataSource;
            }

            return Path.IsPathRooted(builder.DataSource)
                ? builder.DataSource
                : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, builder.DataSource));
        }

        private async Task<TargetTableState> GetTargetTableStateAsync(CancellationToken cancellationToken)
        {
            return new TargetTableState(
                Authors: await _targetContext.Authors.AnyAsync(cancellationToken),
                Cheeps: await _targetContext.Cheeps.AnyAsync(cancellationToken),
                Follows: await _targetContext.UserFollows.AnyAsync(cancellationToken),
                Likes: await _targetContext.Likes.AnyAsync(cancellationToken),
                Roles: await _targetContext.Set<IdentityRole>().AnyAsync(cancellationToken),
                RoleClaims: await _targetContext.Set<IdentityRoleClaim<string>>().AnyAsync(cancellationToken),
                UserClaims: await _targetContext.Set<IdentityUserClaim<string>>().AnyAsync(cancellationToken),
                UserLogins: await _targetContext.Set<IdentityUserLogin<string>>().AnyAsync(cancellationToken),
                UserRoles: await _targetContext.Set<IdentityUserRole<string>>().AnyAsync(cancellationToken),
                UserTokens: await _targetContext.Set<IdentityUserToken<string>>().AnyAsync(cancellationToken));
        }

        private async Task<IdentityImportCounts> CopyIdentityDataAsync(TargetTableState tableState, CancellationToken cancellationToken)
        {
            var roles = await _sourceContext.Set<IdentityRole>().AsNoTracking().ToListAsync(cancellationToken);
            var roleClaims = await _sourceContext.Set<IdentityRoleClaim<string>>().AsNoTracking().ToListAsync(cancellationToken);
            var userClaims = await _sourceContext.Set<IdentityUserClaim<string>>().AsNoTracking().ToListAsync(cancellationToken);
            var userLogins = await _sourceContext.Set<IdentityUserLogin<string>>().AsNoTracking().ToListAsync(cancellationToken);
            var userRoles = await _sourceContext.Set<IdentityUserRole<string>>().AsNoTracking().ToListAsync(cancellationToken);
            var userTokens = await _sourceContext.Set<IdentityUserToken<string>>().AsNoTracking().ToListAsync(cancellationToken);
            var authors = await _sourceContext.Authors.AsNoTracking().ToListAsync(cancellationToken);

            var importedRoles = 0;
            var importedRoleClaims = 0;
            var importedUserClaims = 0;
            var importedUserLogins = 0;
            var importedUserRoles = 0;
            var importedUserTokens = 0;
            var importedAuthors = 0;

            if (!tableState.Roles)
            {
                await BulkInsertAsync(roles.Select(CloneIdentityRole).ToList(), cancellationToken);
                importedRoles = roles.Count;
            }

            if (!tableState.RoleClaims)
            {
                await BulkInsertAsync(roleClaims.Select(CloneRoleClaim).ToList(), cancellationToken);
                importedRoleClaims = roleClaims.Count;
            }

            if (!tableState.UserClaims)
            {
                await BulkInsertAsync(userClaims.Select(CloneUserClaim).ToList(), cancellationToken);
                importedUserClaims = userClaims.Count;
            }

            if (!tableState.UserLogins)
            {
                await BulkInsertAsync(userLogins.Select(CloneUserLogin).ToList(), cancellationToken);
                importedUserLogins = userLogins.Count;
            }

            if (!tableState.UserRoles)
            {
                await BulkInsertAsync(userRoles.Select(CloneUserRole).ToList(), cancellationToken);
                importedUserRoles = userRoles.Count;
            }

            if (!tableState.UserTokens)
            {
                await BulkInsertAsync(userTokens.Select(CloneUserToken).ToList(), cancellationToken);
                importedUserTokens = userTokens.Count;
            }

            if (!tableState.Authors)
            {
                await BulkInsertAsync(authors.Select(CloneAuthor).ToList(), cancellationToken);
                importedAuthors = authors.Count;
            }

            return new IdentityImportCounts(
                importedAuthors,
                importedRoles,
                importedRoleClaims,
                importedUserClaims,
                importedUserLogins,
                importedUserRoles,
                importedUserTokens);
        }

        private async Task<DomainImportCounts> CopyDomainDataAsync(TargetTableState tableState, CancellationToken cancellationToken)
        {
            var cheeps = await _sourceContext.Cheeps.AsNoTracking().ToListAsync(cancellationToken);
            var follows = await _sourceContext.UserFollows.AsNoTracking().ToListAsync(cancellationToken);
            var likes = await _sourceContext.Likes.AsNoTracking().ToListAsync(cancellationToken);

            var importedCheeps = 0;
            var importedFollows = 0;
            var importedLikes = 0;

            if (!tableState.Cheeps)
            {
                await BulkInsertAsync(cheeps.Select(CloneCheep).ToList(), cancellationToken);
                importedCheeps = cheeps.Count;
            }

            if (!tableState.Follows)
            {
                await BulkInsertAsync(follows.Select(CloneUserFollow).ToList(), cancellationToken);
                importedFollows = follows.Count;
            }

            if (!tableState.Likes)
            {
                await BulkInsertAsync(likes.Select(CloneLike).ToList(), cancellationToken);
                importedLikes = likes.Count;
            }

            return new DomainImportCounts(importedCheeps, importedFollows, importedLikes);
        }

        private async Task BulkInsertAsync<TEntity>(IList<TEntity> entities, CancellationToken cancellationToken)
            where TEntity : class
        {
            if (entities.Count == 0)
            {
                return;
            }

            var bulkConfig = new BulkConfig
            {
                BatchSize = 2000,
                PreserveInsertOrder = true,
                SetOutputIdentity = false,
                TrackingEntities = false
            };

            await _targetContext.BulkInsertAsync(entities, bulkConfig, cancellationToken: cancellationToken);
        }

        private async Task ResetIdentitySequencesAsync(CancellationToken cancellationToken)
        {
            await ResetSequenceAsync(SequenceTarget.CheepsCheepId, cancellationToken);
            await ResetSequenceAsync(SequenceTarget.AspNetRoleClaimsId, cancellationToken);
            await ResetSequenceAsync(SequenceTarget.AspNetUserClaimsId, cancellationToken);
        }

        private async Task ResetSequenceAsync(SequenceTarget sequenceTarget, CancellationToken cancellationToken)
        {
            var sql = sequenceTarget switch
            {
                SequenceTarget.CheepsCheepId =>
                    """
                    SELECT setval(
                        pg_get_serial_sequence('"Cheeps"', 'CheepId'),
                        COALESCE(MAX("CheepId"), 1),
                        MAX("CheepId") IS NOT NULL)
                    FROM "Cheeps";
                    """,
                SequenceTarget.AspNetRoleClaimsId =>
                    """
                    SELECT setval(
                        pg_get_serial_sequence('"AspNetRoleClaims"', 'Id'),
                        COALESCE(MAX("Id"), 1),
                        MAX("Id") IS NOT NULL)
                    FROM "AspNetRoleClaims";
                    """,
                SequenceTarget.AspNetUserClaimsId =>
                    """
                    SELECT setval(
                        pg_get_serial_sequence('"AspNetUserClaims"', 'Id'),
                        COALESCE(MAX("Id"), 1),
                        MAX("Id") IS NOT NULL)
                    FROM "AspNetUserClaims";
                    """,
                _ => throw new ArgumentOutOfRangeException(nameof(sequenceTarget), sequenceTarget, "Unknown sequence target.")
            };

            await _targetContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        private static Author CloneAuthor(Author source)
        {
            return new Author
            {
                Id = source.Id,
                Name = source.Name,
                CreationDate = source.CreationDate,
                ProfilePicPath = source.ProfilePicPath,
                karma = source.karma,
                UserName = source.UserName,
                NormalizedUserName = source.NormalizedUserName,
                Email = source.Email,
                NormalizedEmail = source.NormalizedEmail,
                EmailConfirmed = source.EmailConfirmed,
                PasswordHash = source.PasswordHash,
                SecurityStamp = source.SecurityStamp,
                ConcurrencyStamp = source.ConcurrencyStamp,
                PhoneNumber = source.PhoneNumber,
                PhoneNumberConfirmed = source.PhoneNumberConfirmed,
                TwoFactorEnabled = source.TwoFactorEnabled,
                LockoutEnd = source.LockoutEnd,
                LockoutEnabled = source.LockoutEnabled,
                AccessFailedCount = source.AccessFailedCount
            };
        }

        private static Cheep CloneCheep(Cheep source)
        {
            return new Cheep
            {
                CheepId = source.CheepId,
                AuthorId = source.AuthorId,
                Text = source.Text,
                TimeStamp = source.TimeStamp
            };
        }

        private static UserFollow CloneUserFollow(UserFollow source)
        {
            return new UserFollow
            {
                FollowerId = source.FollowerId,
                FolloweeId = source.FolloweeId,
                TimeStamp = source.TimeStamp
            };
        }

        private static Like CloneLike(Like source)
        {
            return new Like
            {
                authorId = source.authorId,
                CheepId = source.CheepId,
                likeStatus = source.likeStatus
            };
        }

        private static IdentityRole CloneIdentityRole(IdentityRole source)
        {
            return new IdentityRole
            {
                Id = source.Id,
                Name = source.Name,
                NormalizedName = source.NormalizedName,
                ConcurrencyStamp = source.ConcurrencyStamp
            };
        }

        private static IdentityRoleClaim<string> CloneRoleClaim(IdentityRoleClaim<string> source)
        {
            return new IdentityRoleClaim<string>
            {
                Id = source.Id,
                RoleId = source.RoleId,
                ClaimType = source.ClaimType,
                ClaimValue = source.ClaimValue
            };
        }

        private static IdentityUserClaim<string> CloneUserClaim(IdentityUserClaim<string> source)
        {
            return new IdentityUserClaim<string>
            {
                Id = source.Id,
                UserId = source.UserId,
                ClaimType = source.ClaimType,
                ClaimValue = source.ClaimValue
            };
        }

        private static IdentityUserLogin<string> CloneUserLogin(IdentityUserLogin<string> source)
        {
            return new IdentityUserLogin<string>
            {
                LoginProvider = source.LoginProvider,
                ProviderKey = source.ProviderKey,
                ProviderDisplayName = source.ProviderDisplayName,
                UserId = source.UserId
            };
        }

        private static IdentityUserRole<string> CloneUserRole(IdentityUserRole<string> source)
        {
            return new IdentityUserRole<string>
            {
                UserId = source.UserId,
                RoleId = source.RoleId
            };
        }

        private static IdentityUserToken<string> CloneUserToken(IdentityUserToken<string> source)
        {
            return new IdentityUserToken<string>
            {
                UserId = source.UserId,
                LoginProvider = source.LoginProvider,
                Name = source.Name,
                Value = source.Value
            };
        }

        private sealed record IdentityImportCounts(
            int Authors,
            int Roles,
            int RoleClaims,
            int UserClaims,
            int UserLogins,
            int UserRoles,
            int UserTokens);

        private sealed record DomainImportCounts(
            int Cheeps,
            int Follows,
            int Likes);

        private enum SequenceTarget
        {
            CheepsCheepId,
            AspNetRoleClaimsId,
            AspNetUserClaimsId
        }

        private sealed record TargetTableState(
            bool Authors,
            bool Cheeps,
            bool Follows,
            bool Likes,
            bool Roles,
            bool RoleClaims,
            bool UserClaims,
            bool UserLogins,
            bool UserRoles,
            bool UserTokens)
        {
            public bool IsFullySeeded =>
                Authors &&
                Cheeps &&
                Follows &&
                Likes &&
                Roles &&
                RoleClaims &&
                UserClaims &&
                UserLogins &&
                UserRoles &&
                UserTokens;
        }
    }
}
