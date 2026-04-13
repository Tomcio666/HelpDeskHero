using System.ComponentModel.DataAnnotations;

namespace HelpDeskHero.Api.Domain;

public sealed class TicketComment
{
    public int Id { get; set; }

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CreatedByDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
}