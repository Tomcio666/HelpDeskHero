using Hangfire;
using HelpDeskHero.Api.BackgroundJobs.Contracts;
using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Api.Infrastructure.Services;
using HelpDeskHero.Shared.Contracts.Common;
using HelpDeskHero.Shared.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace HelpDeskHero.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public TicketsController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TicketDto>>> GetAll([FromQuery] TicketQueryDto query, CancellationToken ct)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => query.PageSize
        };

        var q = _db.Tickets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            q = q.Where(x =>
                x.Number.Contains(query.Search) ||
                x.Title.Contains(query.Search) ||
                x.Description.Contains(query.Search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            q = q.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            q = q.Where(x => x.Priority == query.Priority);
        }

        q = query.SortBy switch
        {
            "Title" => query.Desc ? q.OrderByDescending(x => x.Title) : q.OrderBy(x => x.Title),
            "Priority" => query.Desc
                ? q.OrderByDescending(x => x.Priority == "High" ? 3 : x.Priority == "Medium" ? 2 : 1)
                : q.OrderBy(x => x.Priority == "Low" ? 1 : x.Priority == "Medium" ? 2 : 3),
            _ => query.Desc ? q.OrderByDescending(x => x.CreatedAtUtc) : q.OrderBy(x => x.CreatedAtUtc)
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TicketDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Description = x.Description,
                Status = x.Status,
                Priority = x.Priority,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
                RowVersionBase64 = Convert.ToBase64String(x.RowVersion)
            })
            .ToListAsync(ct);

        return Ok(new PagedResultDto<TicketDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("deleted")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetDeleted(CancellationToken ct)
    {
        var items = await _db.Tickets
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .OrderByDescending(x => x.DeletedAtUtc)
            .Select(x => new TicketDto
            {
                Id = x.Id,
                Number = x.Number,
                Title = x.Title,
                Description = x.Description,
                Status = x.Status,
                Priority = x.Priority,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc,
                RowVersionBase64 = Convert.ToBase64String(x.RowVersion)
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("export")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        CancellationToken ct)
    {
        var query = _db.Tickets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x => x.Priority == priority);
        }

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.Title,
                x.Status,
                x.Priority,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Number,Title,Status,Priority,CreatedAtUtc");

        foreach (var row in rows)
        {
            var title = row.Title.Replace("\"", "\"\"");
            sb.AppendLine($"{row.Id},{row.Number},\"{title}\",{row.Status},{row.Priority},{row.CreatedAtUtc:O}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"tickets-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        return Ok(new TicketDto
        {
            Id = entity.Id,
            Number = entity.Number,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            Priority = entity.Priority,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            RowVersionBase64 = Convert.ToBase64String(entity.RowVersion)
        });
    }

    [HttpPost]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
        {
            return BadRequest(new
            {
                code = "validation_error",
                message = "Title and Description are required."
            });
        }

        var nextNumber = $"HDH-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var createdByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var entity = new Ticket
        {
            Number = nextNumber,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority,
            Status = "New",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _db.Tickets.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Create", "Ticket", entity.Id.ToString(), new { entity.Number, entity.Title }, ct);

        BackgroundJob.Enqueue<INotificationJob>(job =>
            job.SendTicketCreatedNotificationsAsync(entity.Id, default));

        var result = new TicketDto
        {
            Id = entity.Id,
            Number = entity.Number,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            Priority = entity.Priority,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            RowVersionBase64 = Convert.ToBase64String(entity.RowVersion)
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(dto.RowVersionBase64))
        {
            return BadRequest(new
            {
                code = "validation_error",
                message = "RowVersionBase64 is required."
            });
        }

        var originalRowVersion = Convert.FromBase64String(dto.RowVersionBase64);
        _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.Status = dto.Status;
        entity.Priority = dto.Priority;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Update", "Ticket", entity.Id.ToString(), new { entity.Number, entity.Title }, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var entity = await _db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        entity.IsDeleted = true;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.DeletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("SoftDelete", "Ticket", entity.Id.ToString(), new { entity.Number, entity.Title }, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = "CanManageTickets")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!ticket.IsDeleted)
        {
            return BadRequest(new { message = "Ticket is not deleted." });
        }

        ticket.IsDeleted = false;
        ticket.DeletedAtUtc = null;
        ticket.DeletedByUserId = null;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Restore", "Ticket", ticket.Id.ToString(), new { ticket.Number, ticket.Title }, ct);

        return NoContent();
    }
}