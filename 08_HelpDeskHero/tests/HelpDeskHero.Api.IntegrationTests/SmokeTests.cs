using System.Net;
using FluentAssertions;

namespace HelpDeskHero.Api.IntegrationTests;

public sealed class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ShouldBeReachable()
    {
        var response = await _client.GetAsync("/swagger");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.Redirect);
    }
}