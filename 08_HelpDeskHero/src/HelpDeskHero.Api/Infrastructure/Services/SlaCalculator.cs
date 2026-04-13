using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Domain;

namespace HelpDeskHero.Api.Infrastructure.Services;

public sealed class SlaCalculator : ISlaCalculator
{
    public Task ApplySlaAsync(Ticket ticket, CancellationToken ct = default)
    {
        // In a real implementation, this would calculate SLA based on policies
        // For this minimal implementation, we'll just set some default values
        ticket.DueFirstResponseAtUtc = ticket.CreatedAtUtc.AddHours(1);
        ticket.DueResolveAtUtc = ticket.CreatedAtUtc.AddHours(8);

        return Task.CompletedTask;
    }
}