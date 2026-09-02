using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence;
using System;
using System.Threading.Tasks;

namespace WebApplication
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateHostBuilder(args).Build();
            await ApplyMigrations(webHost.Services);
            await webHost.RunAsync();
        }

        private static async Task ApplyMigrations(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<RepositoryDbContext>();

            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "EF migration skipped; using schema bootstrap.");
            }

            try
            {
                await DatabaseBootstrap.EnsureSchemaAndSeedAsync(dbContext);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database bootstrap failed. Check SQL Server connection string in appsettings.json");
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                });
    }
}
