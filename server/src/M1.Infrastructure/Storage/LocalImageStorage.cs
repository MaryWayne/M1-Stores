using M1.Application.Common;
using M1.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace M1.Infrastructure.Storage;

/// <summary>
/// Stores uploads under wwwroot/uploads and serves them as static files.
/// Swap for a CDN-backed IImageStorage (e.g. Cloudinary) in production —
/// container disks are ephemeral.
/// </summary>
public class LocalImageStorage(IConfiguration config) : IImageStorage
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedTypes.Contains(contentType))
            throw new DomainRuleException("Only JPEG, PNG, WebP or GIF images are allowed.");

        var uploadsRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var safeName = $"{Guid.CreateVersion7():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsRoot, safeName);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);

        var baseUrl = (config["App:ApiUrl"] ?? "").TrimEnd('/');
        return $"{baseUrl}/uploads/{safeName}";
    }
}
