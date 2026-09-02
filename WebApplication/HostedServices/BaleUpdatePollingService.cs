using Microsoft.Extensions.Configuration;
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
    /// در حالت Development/Localhost بدون دامنه عمومی، getUpdates را به‌صورت دوره‌ای می‌خواند
    /// و chat_id کاربران را ذخیره می‌کند.
    /// </summary>
    public sealed class BaleUpdatePollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaleUpdatePollingService> _logger;

        public BaleUpdatePollingService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<BaleUpdatePollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = string.Equals(_configuration["Bale:EnablePolling"], "true", StringComparison.OrdinalIgnoreCase);
            if (!enabled)
            {
                _logger.LogInformation("Bale polling disabled. Set Bale:EnablePolling=true to auto-bind chat ids.");
                return;
            }

            var token = _configuration["Bale:BotToken"];
            if (string.IsNullOrWhiteSpace(token) || token == "YOUR_BALE_BOT_TOKEN")
            {
                _logger.LogWarning("Bale polling skipped: BotToken is not configured.");
                return;
            }

            _logger.LogInformation("Bale update polling started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var services = scope.ServiceProvider.GetRequiredService<IServiceManager>();
                    var result = await services.BaleService.PollAndBindChatIdsAsync(stoppingToken);
                    if (result.Success)
                        _logger.LogDebug("Bale poll: {Message}", result.Message);
                    else
                        _logger.LogWarning("Bale poll failed: {Message}", result.Message);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Bale polling error");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
