using System.ComponentModel.DataAnnotations;

namespace HelpDeskHero.Api.Domain;

public sealed class Ticket
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string Number { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "New";

    [Required]
    [StringLength(30)]
    public string Priority { get; set; } = "Medium";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }

    public DateTime? DueFirstResponseAtUtc { get; set; }
    public DateTime? DueResolveAtUtc { get; set; }
    public DateTime? FirstRespondedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime? LastNotifiedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}