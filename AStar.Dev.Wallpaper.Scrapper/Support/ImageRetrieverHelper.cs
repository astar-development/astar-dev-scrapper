namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class ImageRetrieverHelper
{
    public static async Task<byte[]> GetTheImageAsync(string src)
    {
        HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2), };

        HttpResponseMessage response = await client.GetAsync(src);

        return response is { IsSuccessStatusCode: true, } ? await response.Content.ReadAsByteArrayAsync() : [];
    }
}
