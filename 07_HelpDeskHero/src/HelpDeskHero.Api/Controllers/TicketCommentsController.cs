using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HelpDeskHero.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:int}/comments")]
[Authorize]
public sealed class TicketCommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketCommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketCommentDto>>> GetAll(int ticketId, CancellationToken ct)
    {
        var items = await _db.TicketComments
            .AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new TicketCommentDto
            {
                Id = x.Id,
                TicketId = x.TicketId,
                Body = x.Body,
                IsInternal = x.IsInternal,
                CreatedAtUtc = x.CreatedAtUtc,
                CreatedByDisplayName = x.CreatedByDisplayName
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<TicketCommentDto>> Create(
        int ticketId,
        [FromBody] CreateTicketCommentDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(dto.Body))
        {
            return BadRequest(new { message = "Comment body is required." });
        }

        var exists = await _db.Tickets.AnyAsync(x => x.Id == ticketId, ct);
        if (!exists)
        {
            return NotFound();
        }

        var entity = new TicketComment
        {
            TicketId = ticketId,
            Body = dto.Body.Trim(),
            IsInternal = dto.IsInternal,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            CreatedByDisplayName = User.Identity?.Name ?? "unknown"
        };

        _db.TicketComments.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new TicketCommentDto
        {
            Id = entity.Id,
            TicketId = entity.TicketId,
            Body = entity.Body,
            IsInternal = entity.IsInternal,
            CreatedAtUtc = entity.CreatedAtUtc,
            CreatedByDisplayName = entity.CreatedByDisplayName
        });
    }
}