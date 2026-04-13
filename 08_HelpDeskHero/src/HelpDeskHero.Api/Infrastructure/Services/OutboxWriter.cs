using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Domain;
using System.Text.Json;

namespace HelpDeskHero.Api.Infrastructure.Services;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _db;

    public OutboxWriter(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(string type, object payload, CancellationToken ct = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        _db.OutboxMessages.Add(message);
        return _db.SaveChangesAsync(ct);
    }
}