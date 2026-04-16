using System;
using System.IO;
using Chirp.Core.Data;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Chirp.Infrastructure.Data;
using Chirp.Razor.Repositories;
using Microsoft.EntityFrameworkCore;
using Chirp.Application.Services.Implementation;
using Chirp.Application.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Data.Sqlite;
using Npgsql;
using Prometheus;

// -----------------------------------------------------------------------------
// Application configuration entry point for the Chirp web app.
//
// This file configures and initializes the ASP.NET Core application by:
// - Registering application services, repositories, and DbContext instances
//   with the dependency injection container
// - Configuring authentication and identity, including GitHub OAuth login
// - Setting up Entity Framework Core with SQLite and applying pending migrations
// - Defining the HTTP request pipeline and middleware ordering
//
// The code in this file is executed once at application startup and represents
// the composition root of the system, where all infrastructure, services, and
// framework integrations are wired together.
//
// No application or business logic resides here.
// -----------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);
var app = Program.BuildWebApplication(args);
app.Run();

public partial class Program
{
    public static WebApplication BuildWebApplication(
        string[]? args = null,
        bool disableHttpsRedirection = false,
        bool disableExternalAuth = true, //Disables Github authentication
        string? primaryConnectionStringOverride = null,
        string? environmentName = null,
        string? contentRoot = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? Array.Empty<string>(),
            ContentRootPath = contentRoot ?? Directory.GetCurrentDirectory(),
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = environmentName
        });
        
        builder.Services
            .AddAuthentication("BasicAuthentication")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHaHandler>(
                "BasicAuthentication", null);
        
        builder.Services.AddAuthorizationBuilder().AddPolicy("ApiPolicy", policy => policy.AddAuthenticationSchemes("BasicAuthentication").RequireAuthenticatedUser());

        if (!string.IsNullOrWhiteSpace(contentRoot))
        {
            builder.Environment.ContentRootPath = contentRoot;
            builder.Configuration.SetBasePath(contentRoot);
        }

        if (!string.IsNullOrWhiteSpace(primaryConnectionStringOverride))
        {
            builder.Configuration["ConnectionStrings:ChirpPrimaryConnection"] = primaryConnectionStringOverride;
        }

        builder.Services.AddRazorPages(options =>
        {
        });
        builder.Services.AddControllersWithViews();
        builder.Services.AddScoped<ICheepService, CheepService>();
        builder.Services.AddScoped<ICheepRepository, CheepRepository>();
        builder.Services.AddScoped<IAuthorService, AuthorService>();
        builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();


        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
        
        builder.Configuration["ConnectionStrings:LegacySqliteConnection"] =
            ResolveSqliteConnectionString(builder, "LegacySqliteConnection");
        EnsureSqliteDirectory(builder, "LegacySqliteConnection");

        var primaryDatabase = RegisterPrimaryDatabase(builder);

        builder.Services.AddDbContext<SqliteSeedChirpDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("LegacySqliteConnection"))
                .EnableSensitiveDataLogging(false);
        });
        builder.Services.AddScoped<Seeding>();

        builder.Services.AddDefaultIdentity<Author>(
            options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ "; //Allow all characters for username, even " "
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;
            }).AddEntityFrameworkStores<ChirpDBContext>();

        builder.Services.AddDistributedMemoryCache();
        
        //Removed external login possibility.
        
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        // External login providers are disabled; login state is tracked via session.

        //Add user logging for loki/grafana
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
            logging.RequestBodyLogLimit = 4096;
            logging.ResponseBodyLogLimit = 4096;
        });

        
        var app = builder.Build();

        app.Logger.LogInformation(
            "Primary database provider selected: {Provider}. Reason: {Reason}",
            primaryDatabase.Provider,
            primaryDatabase.Reason);

        using (var scope = app.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ChirpDBContext>();

            if (ctx.Database.IsNpgsql())
            {
                var seeding = scope.ServiceProvider.GetRequiredService<Seeding>();
                seeding.EnsureMigratedAndSeededAsync().GetAwaiter().GetResult();
            }
            else
            {
                ctx.Database.EnsureCreated();
            }
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (!disableHttpsRedirection && !app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/Identity/Account/Manage"))
            {
                context.Response.StatusCode = 404;
                return;
            }
            await next();
        });
        
        app.UseHttpLogging();
        app.UseRouting();
        
        app.Use(async (context, next) =>
        {
            var start = DateTime.UtcNow;
            await next();
            var duration = DateTime.UtcNow - start;
            
            //Skip fuckass metrics... OR other requests if needed
            if (context.Request.Path.StartsWithSegments("/metrics"))
            {
                return;
            }
            
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation(
                "REQUEST {Method} {Path} {StatusCode} {Duration}ms from {IP}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration.TotalMilliseconds,
                context.Connection.RemoteIpAddress
            );
        });
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();
        
        app.UseHttpMetrics(options =>
        {
            options.AddCustomLabel("service", context => "MinitwitApi");
        });
        
        app.MapControllers();
        app.MapRazorPages();
        app.MapFallbackToPage("/PublicView");

        app.MapMetrics("/metrics");
        
        return app;
    }
    
    private static PrimaryDatabaseSelection RegisterPrimaryDatabase(WebApplicationBuilder builder)
    {
        var primaryDatabase = ResolvePrimaryDatabase(builder);

        builder.Services.AddDbContext<ChirpDBContext>(options =>
        {
            if (primaryDatabase.Provider == DatabaseProvider.Sqlite)
            {
                options.UseSqlite(primaryDatabase.ConnectionString);
            }
            else
            {
                options.UseNpgsql(primaryDatabase.ConnectionString, npgsql =>
                    npgsql.MigrationsAssembly("Chirp.Infrastructure"));
            }

            options.EnableSensitiveDataLogging(false);
        });

        return primaryDatabase;
    }

    private static PrimaryDatabaseSelection ResolvePrimaryDatabase(WebApplicationBuilder builder)
    {
        var sqliteConnectionString = builder.Configuration.GetConnectionString("LegacySqliteConnection");
        if (string.IsNullOrWhiteSpace(sqliteConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:LegacySqliteConnection is not configured.");
        }

        var configuredPrimaryConnectionString = builder.Configuration.GetConnectionString("ChirpPrimaryConnection");
        if (!string.IsNullOrWhiteSpace(configuredPrimaryConnectionString) &&
            LooksLikeSqliteConnection(configuredPrimaryConnectionString))
        {
            builder.Configuration["ConnectionStrings:ChirpPrimaryConnection"] =
                ResolveSqliteConnectionString(builder, "ChirpPrimaryConnection");
            EnsureSqliteDirectory(builder, "ChirpPrimaryConnection");

            return new PrimaryDatabaseSelection(
                DatabaseProvider.Sqlite,
                builder.Configuration.GetConnectionString("ChirpPrimaryConnection")!,
                "Configured primary connection string is SQLite.");
        }

        if (!TryResolvePostgresConnectionString(builder, out var postgresConnectionString))
        {
            return new PrimaryDatabaseSelection(
                DatabaseProvider.Sqlite,
                sqliteConnectionString,
                "No PostgreSQL connection string was configured; using legacy SQLite.");
        }

        if (CanConnectToPostgres(postgresConnectionString))
        {
            builder.Configuration["ConnectionStrings:ChirpPrimaryConnection"] = postgresConnectionString;
            return new PrimaryDatabaseSelection(
                DatabaseProvider.Postgres,
                postgresConnectionString,
                "Successfully connected to PostgreSQL during startup.");
        }

        return new PrimaryDatabaseSelection(
            DatabaseProvider.Sqlite,
            sqliteConnectionString,
            "PostgreSQL connection string was present, but the startup connectivity check failed; using legacy SQLite.");
    }

    private static bool LooksLikeSqliteConnection(string connectionString)
    {
        return connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveSqliteConnectionString(WebApplicationBuilder builder, string connectionStringName)
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString) || !LooksLikeSqliteConnection(connectionString))
        {
            return connectionString ?? string.Empty;
        }

        var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource) ||
            sqliteBuilder.DataSource == ":memory:" ||
            Path.IsPathRooted(sqliteBuilder.DataSource))
        {
            return connectionString;
        }

        sqliteBuilder.DataSource = Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource));

        return sqliteBuilder.ToString();
    }

    private static bool TryResolvePostgresConnectionString(WebApplicationBuilder builder, out string connectionString)
    {
        connectionString = builder.Environment.IsProduction()
            ? Environment.GetEnvironmentVariable("CHIRP_DB_CONNECTION") ?? string.Empty
            : builder.Configuration.GetConnectionString("ChirpPrimaryConnection") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString) || LooksLikeSqliteConnection(connectionString))
        {
            connectionString = string.Empty;
            return false;
        }

        return true;
    }

    private static bool CanConnectToPostgres(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureSqliteDirectory(WebApplicationBuilder builder, string connectionStringName)
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
        {
            return;
        }

        var dataSource = sqliteBuilder.DataSource;
        var fullPath = Path.IsPathRooted(dataSource) ? dataSource : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dataSource));

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private enum DatabaseProvider
    {
        Sqlite,
        Postgres
    }

    private sealed record PrimaryDatabaseSelection(
        DatabaseProvider Provider,
        string ConnectionString,
        string Reason);
}
