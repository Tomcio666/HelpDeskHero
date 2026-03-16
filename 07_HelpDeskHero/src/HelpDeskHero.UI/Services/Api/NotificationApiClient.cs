using System.Net.Http.Json;
using HelpDeskHero.Shared.Contracts.Notifications;

namespace HelpDeskHero.UI.Services.Api;

public sealed class NotificationApiClient
{
    private readonly HttpClient _http;

    public NotificationApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<UserNotificationDto>> GetMineAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<UserNotificationDto>>("api/notifications/mine", ct);
        return result ?? new List<UserNotificationDto>();
    }

    public async Task MarkAsReadAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/notifications/{id}/read", null, ct);
        response.EnsureSuccessStatusCode();
    }
}