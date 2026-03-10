namespace HelpDeskHero.Api.Infrastructure.Storage;

public static class AttachmentValidation
{
    private static readonly string[] AllowedExtensions =
    [
        ".png", ".jpg", ".jpeg", ".pdf", ".txt", ".docx", ".xlsx"
    ];

    private const long MaxSizeBytes = 10 * 1024 * 1024;

    public static void Validate(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Empty file.");
        }

        if (file.Length > MaxSizeBytes)
        {
            throw new InvalidOperationException("File too large.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("File type not allowed.");
        }
    }
}