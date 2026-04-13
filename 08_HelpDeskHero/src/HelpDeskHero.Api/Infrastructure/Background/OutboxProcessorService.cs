using System.Text.Json;
using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Infrastructure.Background;

public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(IServiceProvider serviceProvider, ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifier = scope.ServiceProvider.GetRequiredService<ITicketLiveNotifier>();

                var messages = await db.OutboxMessages
                    .Where(x => x.ProcessedAtUtc == null && x.RetryCount < 10)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in messages)
                {
                    try
                    {
                        if (msg.Type == "TicketChanged")
                        {
                            var dto = JsonSerializer.Deserialize<TicketLiveUpdateDto>(msg.Payload);
                            if (dto is not null)
                            {
                                await notifier.NotifyTicketChangedAsync(dto, stoppingToken);
                            }
                        }

                        msg.ProcessedAtUtc = DateTime.UtcNow;
                        msg.Error = null;
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount += 1;
                        msg.Error = ex.Message;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}