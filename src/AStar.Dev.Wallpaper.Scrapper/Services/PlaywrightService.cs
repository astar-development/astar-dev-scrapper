using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class PlaywrightService(ScrapeConfiguration scrapeConfiguration) : IPlaywrightService
{
    public async Task<IPage> ConfigurePlaywright(Logger logger)
    {
        using IPlaywright playwright = await Playwright.CreateAsync();

        IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
            Channel = "chrome",
            Args = ["--disable-blink-features=AutomationControlled"],
        });

        IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = scrapeConfiguration.SearchConfiguration.BaseUrl,
            ViewportSize = new ViewportSize { Width = 2440, Height = 1200 },
            Locale = "en-GB",
            TimezoneId = "Europe/London",
        });

        await ApplyCookiesAsync(context, logger);

        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        return page;
    }

    private static async Task ApplyCookiesAsync(IBrowserContext context, Logger logger)
    {
        var chromeCookies = await ChromeCookieExtractor.ExtractAsync("wallhaven.cc", null);
        logger.Information("Extracted {Count} cookies from Chrome profile", chromeCookies.Count);
        var injected = 0;
        foreach (var cookie in chromeCookies)
        {
            try
            {
                await context.AddCookiesAsync([cookie]);
                injected++;
            }
            catch (Exception ex)
            {
                logger.Debug("Skipped cookie '{Name}' ({Domain}): {Message}", cookie.Name, cookie.Domain, ex.Message);
            }
        }

        logger.Information("Injected {Injected}/{Total} cookies", injected, chromeCookies.Count);
    }
}
