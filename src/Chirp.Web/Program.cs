using System;
using System.IO;
using Chirp.Core.Data;
using Chirp.Core.Models;
using Chirp.Core.Repositories;
using Chirp.Razor.Repositories;
using Microsoft.EntityFrameworkCore;
using Chirp.Application.Services.Implementation;
using Chirp.Application.Services.Interface;
using Microsoft.Data.Sqlite;

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
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });

        if (!string.IsNullOrWhiteSpace(contentRoot))
        {
            builder.Environment.ContentRootPath = contentRoot;
            builder.Configuration.SetBasePath(contentRoot);
        }

        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
        {
            builder.Configuration["ConnectionStrings:ChirpDBConnection"] = connectionStringOverride;
        }

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.Environment.EnvironmentName = environmentName;
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
        
        //Ensures sqlite database connectionstring and directory available in docker.
        EnsureSqliteDirectory(builder);

        builder.Services.AddDbContext<ChirpDBContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("ChirpDBConnection"))
                   .EnableSensitiveDataLogging(false);
        });

        builder.Services.AddDefaultIdentity<Author>(
            options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
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

            ctx.Database.Migrate();
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

        app.MapRazorPages();
        app.MapFallbackToPage("/PublicView");

        return app;
    }
    
    //Creates a directory for the SQLite database on containers such as docker.
    private static void EnsureSqliteDirectory(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("ChirpDBConnection");
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
}