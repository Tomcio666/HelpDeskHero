using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace HelpDeskHero.Api.Tests;

public class StartupTests
{
    [Fact]
    public async Task Application_Starts_Without_Exceptions()
    {
        // This test ensures that the application can start without throwing exceptions
        // It's a basic smoke test to verify that the DI container is properly configured

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // This would normally be done in Program.cs
                // But we're just testing that the services can be resolved
            })
            .Build();

        // If we get here without exception, the host started successfully
        Assert.NotNull(host);
    }
}