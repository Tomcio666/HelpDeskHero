using HelpDeskHero.Api.Domain;

namespace HelpDeskHero.Api.Application.Interfaces;

public interface ITicketLiveNotifier
{
    Task NotifyAsync(Ticket ticket, string eventType, CancellationToken ct = default);
}