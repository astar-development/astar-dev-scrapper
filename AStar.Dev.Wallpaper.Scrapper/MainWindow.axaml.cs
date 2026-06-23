using Avalonia.Controls;
using Avalonia.Interactivity;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using AStar.Dev.Infrastructure.FilesDb.Data;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window
{
    private readonly Func<ScrapeConfigurationView> _scrapeConfigViewFactory;
    private readonly ScrapeConfiguration scrapeConfiguration;
    private readonly Logger logger;
    private readonly FilesContext dbContext;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory, ScrapeConfiguration scrapeConfiguration, FilesContext dbContext, Logger logger)
    {
        _scrapeConfigViewFactory = scrapeConfigViewFactory;
        this.scrapeConfiguration = scrapeConfiguration;
        this.logger = logger;
        this.dbContext = dbContext;
        InitializeComponent();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await _scrapeConfigViewFactory().ShowDialog(this);

    private async void OnScrapeSiteClicked(object? sender, RoutedEventArgs e)
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
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
            TimezoneId = "America/New_York",
        });

        await ApplyCookies(context);
        
        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

var configSaver = new ConfigurationSaver(scrapeConfiguration, logger, dbContext);
var tagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely(dbContext);
var tagsTextToIgnore = TagsFactory.LoadTagsTextToIgnore(dbContext);

        var scrapedTagRepository = new ScrapedTagRepository(scrapeConfiguration.ConnectionStrings.Sqlite);
        var imagePage = new ImagePage(
            page,
            scrapeConfiguration,
            tagsToIgnoreCompletely,
            tagsTextToIgnore,
            scrapedTagRepository);
        var fileDetailRepository = new FileDetailRepository(scrapeConfiguration.ConnectionStrings.Sqlite);
        var fileClassificationService = new FileClassificationService(scrapeConfiguration.ConnectionStrings.Sqlite);
        var imagePageService = new ImagePageService(imagePage, fileDetailRepository, fileClassificationService, scrapeConfiguration, logger);

        var searchResultsPage = new SearchResultsPage(page, logger);
        var searchWorkflow = new SearchWorkflow(searchResultsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, logger);
        await searchWorkflow.RunAsync();

        var subscriptionsPage = new SubscriptionsImagesListPage(page, scrapeConfiguration.SearchConfiguration);
        var subscriptionsWorkflow = new SubscriptionsWorkflow(subscriptionsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, logger);
        await subscriptionsWorkflow.RunAsync();

        var topWallpapersPage = new TopWallpapersPage(page, scrapeConfiguration.SearchConfiguration);
        var topWallpapersWorkflow = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration.SearchConfiguration, configSaver, logger);
        await topWallpapersWorkflow.RunAsync();

        await configSaver.SaveUpdatedConfigurationAsync();
    }

    private async Task ApplyCookies(IBrowserContext context)
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
