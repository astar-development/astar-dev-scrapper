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
using AStar.Dev.Infrastructure.FilesDb.Data;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window
{
    private readonly Func<ScrapeConfigurationView> _scrapeConfigViewFactory;
    private readonly ScrapeConfiguration scrapeConfiguration;
    private readonly Logger logger;
    private readonly FilesContext dbContext;
    private CancellationTokenSource? _cts;

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
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

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
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "America/New_York",
            });

            await ApplyCookies(context, scrapeLogger);

            IPage page = await context.NewPageAsync();
            page.SetDefaultTimeout(60_000);

            var configSaver = new ConfigurationSaver(scrapeConfiguration, scrapeLogger, dbContext);
            var tagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely(dbContext);
            var tagsTextToIgnore = TagsFactory.LoadTagsTextToIgnore(dbContext);

            var scrapedTagRepository = new ScrapedTagRepository(scrapeConfiguration.ConnectionStrings.Sqlite);
            var imagePage = new ImagePage(page, scrapeConfiguration, tagsToIgnoreCompletely, tagsTextToIgnore, scrapedTagRepository);
            var fileDetailRepository = new FileDetailRepository(scrapeConfiguration.ConnectionStrings.Sqlite);
            var fileClassificationService = new FileClassificationService(scrapeConfiguration.ConnectionStrings.Sqlite);
            var imagePageService = new ImagePageService(imagePage, fileDetailRepository, fileClassificationService, scrapeConfiguration, scrapeLogger);

            var searchResultsPage = new SearchResultsPage(page, scrapeLogger);
            var searchWorkflow = new SearchWorkflow(searchResultsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, scrapeLogger);
            await searchWorkflow.RunAsync(ct);

            var subscriptionsPage = new SubscriptionsImagesListPage(page, scrapeConfiguration.SearchConfiguration);
            var subscriptionsWorkflow = new SubscriptionsWorkflow(subscriptionsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configSaver, scrapeLogger);
            await subscriptionsWorkflow.RunAsync(ct);

            var topWallpapersPage = new TopWallpapersPage(page, scrapeConfiguration.SearchConfiguration);
            var topWallpapersWorkflow = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration.SearchConfiguration, configSaver, scrapeLogger);
            await topWallpapersWorkflow.RunAsync(ct);

            await configSaver.SaveUpdatedConfigurationAsync();
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
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => _cts?.Cancel();

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.Post(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });

    private async Task ApplyCookies(IBrowserContext context, Logger scrapeLogger)
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
