using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication.HostedServices
{
    /// <summary>
    /// یادآوری‌های ارسالی قدیمی‌تر از سه ماه را از تاریخچه پاک می‌کند.
    /// </summary>
    public sealed class ReminderLogCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderLogCleanupService> _logger;

        public ReminderLogCleanupService(IServiceScopeFactory scopeFactory, ILogger<ReminderLogCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var services = scope.ServiceProvider.GetRequiredService<IServiceManager>();
                    var removed = await services.NotificationSchedulerService.PurgeOldReminderLogsAsync(stoppingToken);
                    if (removed > 0)
                        _logger.LogInformation("Removed {Count} reminder logs older than three months.", removed);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Reminder log cleanup failed.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
