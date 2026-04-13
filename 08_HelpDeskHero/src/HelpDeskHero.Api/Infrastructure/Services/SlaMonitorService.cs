using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Domain;

namespace HelpDeskHero.Api.Infrastructure.Services;

public sealed class SlaMonitorService : ISlaMonitorService
{
    public Task MonitorAsync(Ticket ticket, CancellationToken ct = default)
    {
        // In a real implementation, this would monitor SLA breaches
        // For this minimal implementation, we'll just return immediately
        return Task.CompletedTask;
    }
}