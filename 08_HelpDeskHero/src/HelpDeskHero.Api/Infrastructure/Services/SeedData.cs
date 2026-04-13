using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Infrastructure.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Manager", "Agent", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(
            userManager,
            "admin",
            "admin@helpdeskhero.local",
            "System Admin",
            "Admin1234",
            ["Admin", "Agent"]);

        await EnsureUserAsync(
            userManager,
            "manager",
            "manager@helpdeskhero.local",
            "Support Manager",
            "Manager1234",
            ["Manager"]);

        await EnsureUserAsync(
            userManager,
            "agent1",
            "agent1@helpdeskhero.local",
            "Support Agent 1",
            "Agent1234",
            ["Agent"]);

        await EnsureUserAsync(
            userManager,
            "agent2",
            "agent2@helpdeskhero.local",
            "Support Agent 2",
            "Agent1234",
            ["Agent"]);

        await EnsureUserAsync(
            userManager,
            "user",
            "user@helpdeskhero.local",
            "Basic User",
            "User1234",
            ["User"]);

        if (!await db.TicketSlaPolicies.AnyAsync())
        {
            db.TicketSlaPolicies.AddRange(
                new TicketSlaPolicy
                {
                    Name = "Low default SLA",
                    Priority = "Low",
                    FirstResponseMinutes = 240,
                    ResolveMinutes = 2880,
                    IsActive = true
                },
                new TicketSlaPolicy
                {
                    Name = "Medium default SLA",
                    Priority = "Medium",
                    FirstResponseMinutes = 60,
                    ResolveMinutes = 480,
                    IsActive = true
                },
                new TicketSlaPolicy
                {
                    Name = "High default SLA",
                    Priority = "High",
                    FirstResponseMinutes = 15,
                    ResolveMinutes = 120,
                    IsActive = true
                });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string email,
        string displayName,
        string password,
        IEnumerable<string> roles)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                DisplayName = displayName,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(user, roles);
            }
        }
    }
}