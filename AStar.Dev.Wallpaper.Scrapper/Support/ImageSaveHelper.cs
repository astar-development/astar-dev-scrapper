using System.Text.RegularExpressions;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal class ImageSaveHelper
{
    public static async Task SaveImage(byte[] image, string imageNameWithPath)
    {
        var invalidFileChars = Path.GetInvalidFileNameChars();
        imageNameWithPath = Regex.Replace(imageNameWithPath, """[^\u0000-\u007F]+""", string.Empty);

        foreach(var invalidFileChar in invalidFileChars) _ = imageNameWithPath.Replace(invalidFileChar, ' ');

        imageNameWithPath = imageNameWithPath.Replace("\"", "'").Replace("|", string.Empty).Replace("@", string.Empty).Replace("煙", string.Empty);

        if(imageNameWithPath.LastIndexOf(":", StringComparison.Ordinal) > 2) imageNameWithPath = imageNameWithPath[..2] + imageNameWithPath[2..].Replace(":", "_");

        if(image.Length > 0) await File.WriteAllBytesAsync(imageNameWithPath, image);
    }
}
