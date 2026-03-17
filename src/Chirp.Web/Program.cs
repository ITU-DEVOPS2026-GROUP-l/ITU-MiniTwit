using System;
using System.IO;
using Chirp.Core.Data;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Chirp.Razor.Repositories;
using Microsoft.EntityFrameworkCore;
using Chirp.Application.Services.Implementation;
using Chirp.Application.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Data.Sqlite;
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
        string? connectionStringOverride = null,
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

        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
        {
            builder.Configuration["ConnectionStrings:ChirpDBConnection"] = connectionStringOverride;
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
        
        var connectionString = builder.Configuration.GetConnectionString("ChirpDBConnection")
            ?? throw new InvalidOperationException("Connection string 'ChirpDBConnection' is not configured.");
        var databaseProvider = GetDatabaseProvider(builder.Configuration);

        if (databaseProvider == DatabaseProvider.Sqlite)
        {
            EnsureSqliteDirectory(builder, connectionString);
        }

        builder.Services.AddDbContext<ChirpDBContext>(options =>
        {
            switch (databaseProvider)
            {
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString);
                    break;
                case DatabaseProvider.Sqlite:
                default:
                    options.UseSqlite(connectionString);
                    break;
            }

            options.EnableSensitiveDataLogging(false);
        });

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

        
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ChirpDBContext>();

            if (databaseProvider == DatabaseProvider.PostgreSql)
            {
                ctx.Database.EnsureCreated();
            }
            else
            {
                ctx.Database.Migrate();
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
        app.UseRouting();

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
    
    //Creates a directory for the SQLite database on containers such as docker.
    private static void EnsureSqliteDirectory(WebApplicationBuilder builder, string connectionString)
    {
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
