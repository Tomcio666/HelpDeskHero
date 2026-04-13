using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Domain;

namespace HelpDeskHero.Api.Infrastructure.Services;

public sealed class TicketLiveNotifier : ITicketLiveNotifier
{
    public Task NotifyAsync(Ticket ticket, string eventType, CancellationToken ct = default)
    {
        // In a real implementation, this would send live notifications
        // For this minimal implementation, we'll just return immediately
        return Task.CompletedTask;
    }
}