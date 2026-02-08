using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chirp.Core.Data;
using Chirp.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace nPlaywrightTests
{
    public class TestChirpWebFactory : IAsyncDisposable
    {
        private WebApplication? _app;
        private string? _tempDbPath;

        public string BaseAddress { get; private set; } = string.Empty;

        public async Task StartAsync()
        {
            if (_app != null)
            {
                return;
            }

            _tempDbPath = Path.Combine(Path.GetTempPath(), $"chirp-playwright-{Guid.NewGuid():N}.db");
            var connectionString = $"Data Source={_tempDbPath}";
            var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Chirp.Web"));

            _app = global::Program.BuildWebApplication(
                new[] { "--urls", "http://127.0.0.1:0" },
                environmentName: Environments.Development,
                disableExternalAuth: true,
                contentRoot: contentRoot);
            await _app.StartAsync();

            await SeedTestDataAsync();

            var endpointDataSource = _app.Services.GetRequiredService<EndpointDataSource>();
            Console.WriteLine($"Endpoint count: {endpointDataSource.Endpoints.Count}");

            foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
            {
                Console.WriteLine($"Route pattern: {endpoint.RoutePattern.RawText}");
            }
            var address = _app.Urls.First();
            BaseAddress = address.EndsWith("/", StringComparison.Ordinal) ? address : address + "/";
        }

        private async Task SeedTestDataAsync()
        {
            if (_app == null)
            {
                return;
            }

            using var scope = _app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Author>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChirpDBContext>();

            var philip = await EnsureUserAsync(userManager, "phqu@itu.dk", "philip", "Dinmor123!");
            var official = await EnsureUserAsync(userManager, "official@chirp.test", "OfficialChutney", "Chirp123!");

            if (official != null)
            {
                var hasCheep = await dbContext.Cheeps.AnyAsync(c =>
                    c.AuthorId == official.Id && c.Text == "Adhede");

                if (!hasCheep)
                {
                    dbContext.Cheeps.Add(new Cheep
                    {
                        AuthorId = official.Id,
                        Author = official,
                        Text = "Adhede",
                        TimeStamp = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private static async Task<Author?> EnsureUserAsync(
            UserManager<Author> userManager,
            string email,
            string name,
            string password)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return existing;
            }

            var author = new Author
            {
                UserName = email,
                Email = email,
                Name = name
            };

            var result = await userManager.CreateAsync(author, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed test user {email}: {errors}");
            }

            return author;
        }

        public async ValueTask DisposeAsync()
        {
            if (_app == null)
            {
                return;
            }

            await _app.StopAsync();
            await _app.DisposeAsync();

            if (!string.IsNullOrWhiteSpace(_tempDbPath) && File.Exists(_tempDbPath))
            {
                try
                {
                    File.Delete(_tempDbPath);
                }
                catch (IOException)
                {
                    // If SQLite still has a handle, surface a clean test tear down.
                }
            }
        }
    }
}
