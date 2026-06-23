using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window
{
    private readonly Func<ScrapeConfigurationView> scrapeConfigViewFactory;
    private readonly ScrapeConfiguration scrapeConfiguration;
    private readonly Logger logger;
    private readonly IScrapedTagRepository scrapedTagRepository;
    private readonly IFileDetailRepository fileDetailRepository;
    private readonly FileClassificationService fileClassificationService;
    private readonly ConfigurationSaver configurationSaver;
    private readonly TagsToIgnoreCompletely tagsToIgnoreCompletely;
    private readonly TagsTextToIgnore tagsTextToIgnore;
    private CancellationTokenSource? cts;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory, ScrapeConfiguration scrapeConfiguration, Logger logger, IScrapedTagRepository scrapedTagRepository, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ConfigurationSaver configurationSaver, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore)
    {
        this.scrapeConfigViewFactory = scrapeConfigViewFactory;
        this.scrapeConfiguration = scrapeConfiguration;
        this.logger = logger;
        this.scrapedTagRepository = scrapedTagRepository;
        this.fileDetailRepository = fileDetailRepository;
        this.fileClassificationService = fileClassificationService;
        this.configurationSaver = configurationSaver;
        this.tagsToIgnoreCompletely = tagsToIgnoreCompletely;
        this.tagsTextToIgnore = tagsTextToIgnore;
        InitializeComponent();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await scrapeConfigViewFactory().ShowDialog(this);

    private async void OnScrapeSiteClicked(object? sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        ScrapeSiteButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Text = string.Empty;

        try
        {
            using var scrapeLogger = new LoggerConfiguration()
                .WriteTo.Logger(logger)
                .WriteTo.Sink(new StatusLogSink(UpdateStatus))
                .MinimumLevel.Information()
                .CreateLogger();

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

            await ApplyCookiesAsync(context, scrapeLogger);

            IPage page = await context.NewPageAsync();
            page.SetDefaultTimeout(60_000);

            var imagePage = new ImagePage(page, scrapeConfiguration, tagsToIgnoreCompletely, tagsTextToIgnore, scrapedTagRepository);
            var imagePageService = new ImagePageService(imagePage, fileDetailRepository, fileClassificationService, scrapeConfiguration, scrapeLogger);

            await RunSearchAsync(scrapeLogger, page, imagePageService, ct);
            await RunSubscriptionsAsync(scrapeLogger, page, imagePageService, ct);
            await RunTopWallpapersAsync(scrapeLogger, page, imagePageService, ct);

            await configurationSaver.SaveUpdatedConfigurationAsync();
            UpdateStatus("Scrape completed.");
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("Scrape cancelled.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            logger.Error(ex, "Scrape failed");
        }
        finally
        {
            ScrapeSiteButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            cts?.Dispose();
            cts = null;
        }
    }

    private async Task RunTopWallpapersAsync(Logger scrapeLogger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var topWallpapersPage = new TopWallpapersPage(page, scrapeConfiguration.SearchConfiguration);
        var topWallpapersWorkflow = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration.SearchConfiguration, configurationSaver, scrapeLogger);
        await topWallpapersWorkflow.RunAsync(ct);
    }

    private async Task RunSubscriptionsAsync(Logger scrapeLogger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var subscriptionsPage = new SubscriptionsImagesListPage(page, scrapeConfiguration.SearchConfiguration);
        var subscriptionsWorkflow = new SubscriptionsWorkflow(subscriptionsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, scrapeLogger);
        await subscriptionsWorkflow.RunAsync(ct);
    }

    private async Task RunSearchAsync(Logger scrapeLogger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var searchResultsPage = new SearchResultsPage(page, scrapeLogger);
        var searchWorkflow = new SearchWorkflow(searchResultsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, scrapeLogger);
        await searchWorkflow.RunAsync(ct);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => cts?.Cancel();

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.Post(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });

    private static async Task ApplyCookiesAsync(IBrowserContext context, Logger scrapeLogger)
    {
        var chromeCookies = await ChromeCookieExtractor.ExtractAsync("wallhaven.cc", null);
        scrapeLogger.Information("Extracted {Count} cookies from Chrome profile", chromeCookies.Count);
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
                scrapeLogger.Debug("Skipped cookie '{Name}' ({Domain}): {Message}", cookie.Name, cookie.Domain, ex.Message);
            }
        }

        scrapeLogger.Information("Injected {Injected}/{Total} cookies", injected, chromeCookies.Count);
    }

    private sealed class StatusLogSink(Action<string> onMessage) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => onMessage(logEvent.RenderMessage());
    }
}
