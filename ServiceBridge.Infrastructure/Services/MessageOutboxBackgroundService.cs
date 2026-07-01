using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;

namespace ServiceBridge.Infrastructure.Services;

public class MessageOutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessageOutboxSettings _settings;
    private readonly ILogger<MessageOutboxBackgroundService> _logger;

    public MessageOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MessageOutboxSettings> settings,
        ILogger<MessageOutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Message outbox processor is disabled.");
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IMessageOutboxProcessor>();

                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Message outbox processor loop failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }
}
