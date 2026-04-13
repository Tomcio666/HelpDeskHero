using System.ComponentModel.DataAnnotations;

namespace HelpDeskHero.Api.Domain;

public sealed class TicketAttachment
{
    public int Id { get; set; }

    [Required]
    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(520)]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
}