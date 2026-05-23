using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

(ScrapeConfiguration seedConfig, IConfigurationRoot configuration) = ConfigurationFactory.Configuration();

Logger logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithExceptionDetails()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

await using var dbContext = new FilesContext(new DbContextOptions<FilesContext>());
dbContext.Database.Migrate();

await DataSeed.Seed(seedConfig, logger, dbContext);

var scrapeConfigEntity = await dbContext.ScrapeConfiguration
                                        .Include(e => e.ConnectionStrings)
                                        .Include(e => e.UserConfiguration)
                                        .Include(e => e.SearchConfiguration).ThenInclude(sc => sc.SearchCategories)
                                        .Include(e => e.ScrapeDirectories)
                                        .SingleAsync();

ScrapeConfiguration scrapeConfiguration = scrapeConfigEntity.ToAppModel();

var tagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely(dbContext);
var tagsTextToIgnore       = TagsFactory.LoadTagsTextToIgnore(dbContext);

var configSaver = new ConfigurationSaver(scrapeConfiguration, logger, dbContext);

using IPlaywright playwright = await Playwright.CreateAsync();

IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    SlowMo   = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
    Channel  = "chrome",
    Args     = ["--disable-blink-features=AutomationControlled"],
});

IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    BaseURL    = scrapeConfiguration.SearchConfiguration.BaseUrl,
    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
    Locale     = "en-US",
    TimezoneId = "America/New_York",
});

var chromeCookies = await ChromeCookieExtractor.ExtractAsync("wallhaven.cc", logger);
logger.Information("Extracted {Count} cookies from Chrome profile", chromeCookies.Count);
var injected = 0;
foreach(var cookie in chromeCookies)
{
    try
    {
        await context.AddCookiesAsync([cookie]);
        injected++;
    }
    catch(Exception ex)
    {
        logger.Debug("Skipped cookie '{Name}' ({Domain}): {Message}", cookie.Name, cookie.Domain, ex.Message);
    }
}
logger.Information("Injected {Injected}/{Total} cookies", injected, chromeCookies.Count);

IPage page = await context.NewPageAsync();
page.SetDefaultTimeout(60_000);

var loginPage = new LoginPage(page, scrapeConfiguration.SearchConfiguration);
var imagePage = new ImagePage(
    page,
    scrapeConfiguration,
    tagsToIgnoreCompletely,
    tagsTextToIgnore);
var fileDetailRepository = new FileDetailRepository(scrapeConfiguration.ConnectionStrings.Sqlite);
var imagePageService     = new ImagePageService(imagePage, fileDetailRepository, scrapeConfiguration, logger);

// await loginPage.GoToLoginPageAsync();
// if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
// {
//     logger.Information("Cookie session invalid or expired — logging in");
//     await loginPage.LoginAsync(scrapeConfiguration.UserConfiguration.Username, scrapeConfiguration.UserConfiguration.Password);
//     await loginPage.ConfirmLoggedInAsync($"{scrapeConfiguration.SearchConfiguration.BaseUrl}/");
// }
// else
// {
//     logger.Information("Cookie session valid — skipping login");
// }

var runAll           = args.Length == 0;
var runSearch        = runAll || args.Contains("search",        StringComparer.OrdinalIgnoreCase);
var runSubscriptions = runAll || args.Contains("subscriptions", StringComparer.OrdinalIgnoreCase);
var runTopWallpapers = runAll || args.Contains("topwallpapers", StringComparer.OrdinalIgnoreCase);

if(runSearch)
{
    var searchResultsPage = new SearchResultsPage(page, logger);
    var workflow          = new SearchWorkflow(searchResultsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, logger);
    await workflow.RunAsync();
}

if(runSubscriptions)
{
    var subscriptionsPage = new SubscriptionsImagesListPage(page, scrapeConfiguration.SearchConfiguration);
    var workflow          = new SubscriptionsWorkflow(subscriptionsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, logger);
    await workflow.RunAsync();
}

if(runTopWallpapers)
{
    var topWallpapersPage = new TopWallpapersPage(page, scrapeConfiguration.SearchConfiguration);
    var workflow          = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration.SearchConfiguration, configSaver, logger);
    await workflow.RunAsync();
}

await configSaver.SaveUpdatedConfigurationAsync();
logger.Information("Shutting down...");
