using HelpDeskHero.Api.Controllers;
using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Api.Infrastructure.Services;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HelpDeskHero.Api.Tests;

public class TicketControllerTests
{
    [Fact]
    public async Task CreateTicket_ReturnsCreatedResult()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var controller = new TicketsController(
            serviceProvider.GetRequiredService<AppDbContext>(),
            serviceProvider.GetRequiredService<AuditService>(),
            serviceProvider.GetRequiredService<ISlaCalculator>(),
            serviceProvider.GetRequiredService<ITicketAssignmentService>(),
            serviceProvider.GetRequiredService<IOutboxWriter>());

        var dto = new CreateTicketDto
        {
            Title = "Test Ticket",
            Description = "This is a test ticket description"
        };

        // Act
        var result = await controller.Create(dto, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task GetTicketById_ReturnsOkResult()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var controller = new TicketsController(
            serviceProvider.GetRequiredService<AppDbContext>(),
            serviceProvider.GetRequiredService<AuditService>(),
            serviceProvider.GetRequiredService<ISlaCalculator>(),
            serviceProvider.GetRequiredService<ITicketAssignmentService>(),
            serviceProvider.GetRequiredService<IOutboxWriter>());

        // First create a ticket
        var dto = new CreateTicketDto
        {
            Title = "Test Ticket",
            Description = "This is a test ticket description"
        };

        var createResult = await controller.Create(dto, CancellationToken.None);
        var createdTicket = (CreatedAtActionResult)createResult.Result;
        var ticketDto = (TicketDto)createdTicket.Value!;

        // Act
        var result = await controller.GetById(ticketDto.Id, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        services.AddScoped<AuditService>();
        services.AddScoped<ISlaCalculator, SlaCalculator>();
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services.BuildServiceProvider();
    }
}