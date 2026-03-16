using System.Net.Http.Json;
using HelpDeskHero.Shared.Contracts.Audit;

namespace HelpDeskHero.UI.Services.Api;

public sealed class AuditApiClient
{
    private readonly HttpClient _http;

    public AuditApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AuditLogListItemDto>?> GetAsync(
        string? action,
        string? entityName,
        string? performedBy,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var url =
            $"api/audit?action={Uri.EscapeDataString(action ?? string.Empty)}" +
            $"&entityName={Uri.EscapeDataString(entityName ?? string.Empty)}" +
            $"&performedBy={Uri.EscapeDataString(performedBy ?? string.Empty)}" +
            $"&fromUtc={(fromUtc.HasValue ? Uri.EscapeDataString(fromUtc.Value.ToString("O")) : string.Empty)}" +
            $"&toUtc={(toUtc.HasValue ? Uri.EscapeDataString(toUtc.Value.ToString("O")) : string.Empty)}";

        return await _http.GetFromJsonAsync<IReadOnlyList<AuditLogListItemDto>>(url, ct);
    }
}