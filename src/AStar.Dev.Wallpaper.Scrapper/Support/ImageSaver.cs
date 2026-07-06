using System.IO.Abstractions;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>The production <see cref="IImageSaver" />, backed by an injected <see cref="IFileSystem" />, frozen from the historical <c>ImageSaveHelper</c> behaviour.</summary>
public sealed class ImageSaver(IFileSystem fileSystem) : IImageSaver
{
    /// <inheritdoc />
    public async Task SaveAsync(byte[] image, string path)
    {
        string cleanedPath = path.CleanPath();

        if (cleanedPath.LastIndexOf(':') > 2) cleanedPath = cleanedPath[..2] + cleanedPath[2..].Replace(":", "_");

        if (image.Length > 0) await fileSystem.File.WriteAllBytesAsync(cleanedPath, image).ConfigureAwait(false);
    }
}
