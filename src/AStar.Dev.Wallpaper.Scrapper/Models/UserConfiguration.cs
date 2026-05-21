namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class UserConfiguration
{
    public string LoginEmailAddress { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // Full Cookie header value from browser DevTools — required for subscription feed access.
    // Copy from: DevTools → Network → any wallhaven.cc request → Request Headers → Cookie
    public string SessionCookie { get; set; } = string.Empty;
}
