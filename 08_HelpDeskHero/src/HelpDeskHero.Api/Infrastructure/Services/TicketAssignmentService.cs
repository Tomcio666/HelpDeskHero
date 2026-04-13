using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Domain;

namespace HelpDeskHero.Api.Infrastructure.Services;

public sealed class TicketAssignmentService : ITicketAssignmentService
{
    public Task<string?> AssignAsync(Ticket ticket, CancellationToken ct = default)
    {
        // In a real implementation, this would assign tickets based on policies
        // For this minimal implementation, we'll just return null (no assignment)
        return Task.FromResult<string?>(null);
    }
}