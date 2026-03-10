namespace HelpDeskHero.Api.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public LocalFileStorage(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    public async Task<StoredFileResult> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        var configuredRoot = _config["FileStorage:RootPath"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(_env.ContentRootPath, "App_Data", "attachments")
            : Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(_env.ContentRootPath, configuredRoot);

        Directory.CreateDirectory(root);

        var extension = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(root, safeName);

        await using var stream = File.Create(path);
        await file.CopyToAsync(stream, ct);

        return new StoredFileResult
        {
            OriginalFileName = file.FileName,
            StoredFileName = safeName,
            RelativePath = safeName,
            ContentType = file.ContentType,
            SizeBytes = file.Length
        };
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var configuredRoot = _config["FileStorage:RootPath"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(_env.ContentRootPath, "App_Data", "attachments")
            : Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(_env.ContentRootPath, configuredRoot);

        var path = Path.Combine(root, relativePath);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var configuredRoot = _config["FileStorage:RootPath"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(_env.ContentRootPath, "App_Data", "attachments")
            : Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(_env.ContentRootPath, configuredRoot);

        var path = Path.Combine(root, relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}