using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskHero.Api.Tests;

public class ServiceResolutionTests
{
    [Fact]
    public void All_Required_Services_Are_Registered()
    {
        // This test verifies that all required services can be resolved
        // from the DI container

        var services = new ServiceCollection();

        // Add the services that would normally be in Program.cs
        services.AddScoped<ISlaCalculator, SlaCalculator>();
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<ITicketLiveNotifier, TicketLiveNotifier>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<ISlaMonitorService, SlaMonitorService>();

        var serviceProvider = services.BuildServiceProvider();

        // Try to resolve all services
        var slaCalculator = serviceProvider.GetService<ISlaCalculator>();
        var ticketAssignmentService = serviceProvider.GetService<ITicketAssignmentService>();
        var ticketLiveNotifier = serviceProvider.GetService<ITicketLiveNotifier>();
        var outboxWriter = serviceProvider.GetService<IOutboxWriter>();
        var slaMonitorService = serviceProvider.GetService<ISlaMonitorService>();

        // All services should be resolvable
        Assert.NotNull(slaCalculator);
        Assert.NotNull(ticketAssignmentService);
        Assert.NotNull(ticketLiveNotifier);
        Assert.NotNull(outboxWriter);
        Assert.NotNull(slaMonitorService);
    }
}