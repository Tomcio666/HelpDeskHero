using System.Net.Http.Json;

namespace HelpDeskHero.Api.Infrastructure.Notifications;

public sealed class WebhookNotificationSender : INotificationSender
{
    private readonly HttpClient _http;

    public WebhookNotificationSender(HttpClient http)
    {
        _http = http;
    }

    public NotificationChannel Channel => NotificationChannel.Webhook;

    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        var payload = new { text = $"{message.Subject}: {message.Body}" };
        await _http.PostAsJsonAsync("https://example.invalid/webhook", payload, ct);
    }
}