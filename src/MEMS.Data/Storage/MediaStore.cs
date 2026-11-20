using System.Text.RegularExpressions;
using MEMS.Data.Seeding;
using MEMS.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

namespace MEMS.Data.Storage;

public sealed record StoredMedia(string Path, string ContentType);

public sealed partial class MediaStore(IConfiguration configuration, IWebHostEnvironment environment)
{
    public const int MaxBytes = 2 * 1024 * 1024;
    private readonly string root = Path.Combine(configuration["Mems:LocalStorage"]
        ?? throw new InvalidOperationException("Configure application-owned local media storage."), "media", "tenants");
    private readonly string bundled = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "images");
    private static readonly HashSet<string> Portraits = DemoCandidates.All.Concat(RichmondCandidates.All).Select(x => x.PhotoFileName).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> Logos = new[] { "burnaby", "surrey", "richmond", "pitt-meadows", "maple-ridge" }.Select(x => $"demo-{x}.svg").ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex("^[a-f0-9]{32}\\.png$")] private static partial Regex GeneratedName();

    public async Task<string> SaveAsync(int tenantId, IFormFile upload, CancellationToken cancellationToken)
    {
        if (upload.Length > MaxBytes) throw new ApiFault(413, "Images must not exceed 2 MB.");
        if (upload.Length <= 0) throw new ApiFault(400, "Choose a non-empty JPEG or PNG image.");
        if (upload.FileName.Contains('/') || upload.FileName.Contains('\\')) throw new ApiFault(400, "Use a plain image filename.");
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg")) throw new ApiFault(400, "Only JPEG and PNG images are accepted.");
        using var input = new MemoryStream();
        await using (var source = upload.OpenReadStream())
        {
            var buffer = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (input.Length + count > MaxBytes) throw new ApiFault(413, "Images must not exceed 2 MB.");
                await input.WriteAsync(buffer.AsMemory(0,count), cancellationToken);
            }
        }
        using var data = SKData.CreateCopy(input.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null) throw new ApiFault(400, "The file is not a valid JPEG or PNG image.");
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 6000 || info.Height > 6000 || (long)info.Width * info.Height > 16000000)
            throw new ApiFault(400, "Image dimensions must not exceed 6000 pixels or 16 megapixels.");
        if ((extension == ".png" && codec.EncodedFormat != SKEncodedImageFormat.Png) ||
            (extension != ".png" && codec.EncodedFormat != SKEncodedImageFormat.Jpeg))
            throw new ApiFault(400, "Image contents must match the JPEG or PNG extension.");
        if (codec.FrameCount > 1) throw new ApiFault(400, "Animated images are not accepted.");
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new ApiFault(400, "The image is damaged or invalid.");
        var ratio = Math.Min(1d, 1200d / Math.Max(info.Width,info.Height));
        using var resized = bitmap.Resize(new SKImageInfo(Math.Max(1,(int)(info.Width*ratio)),Math.Max(1,(int)(info.Height*ratio))), new SKSamplingOptions(SKFilterMode.Linear));
        using var image = SKImage.FromBitmap(resized);
        using var png = image.Encode(SKEncodedImageFormat.Png,100);
        var directory = Path.Combine(root,tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        var name = Guid.NewGuid().ToString("N") + ".png";
        await File.WriteAllBytesAsync(Path.Combine(directory,name),png.ToArray(),cancellationToken);
        return name;
    }

    public StoredMedia Read(int tenantId, string? name, bool logo)
    {
        if (name is null) throw new ApiFault(404, "Image not found.");
        string path;
        string type;
        if (GeneratedName().IsMatch(name)) { path = Path.Combine(root,tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture),name); type = "image/png"; }
        else if ((logo ? Logos : Portraits).Contains(name))
        {
            path = Path.Combine(bundled,logo ? "municipalities" : "demo-candidates",name);
            type = name.EndsWith(".svg",StringComparison.Ordinal) ? "image/svg+xml" : "image/jpeg";
        }
        else throw new ApiFault(404, "Image not found.");
        if (!File.Exists(path)) throw new ApiFault(404, "Image not found.");
        return new(path,type);
    }

    public void DeleteUpload(int tenantId, string? name)
    {
        if (name is not null && GeneratedName().IsMatch(name))
            File.Delete(Path.Combine(root,tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture),name));
    }
}
