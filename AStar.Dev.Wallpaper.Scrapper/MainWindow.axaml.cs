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
using AStar.Dev.FunctionalParadigm;

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
    private readonly IPlaywrightService playwrightService;
    private readonly IImagePageResultFunctional imagePageResultFunctional;
    private CancellationTokenSource? cts;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory,IImagePageResultFunctional imagePageResultFunctional, IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, Logger logger, IScrapedTagRepository scrapedTagRepository, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ConfigurationSaver configurationSaver, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore)
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
        this.playwrightService = playwrightService;
        this.imagePageResultFunctional = imagePageResultFunctional;
        InitializeComponent();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await scrapeConfigViewFactory().ShowDialog(this);

    private async void OnScrapeSiteNewClicked(object? sender, RoutedEventArgs e)
    {
        var ct = ResetCancellationTokenSource();
        DisableControlsAndClearStatus();
        using var scrapeLogger = GetScrapeLoggerForDisplaySync();
        scrapeLogger.Information("Starting scrape...");
        scrapeLogger.Information("Configuring Playwright...");
        IPage page = await playwrightService.ConfigurePlaywright(scrapeLogger);
        scrapeLogger.Information("Playwright configured. Starting scrape...");
        await imagePageResultFunctional.GetImagePagesAsync(scrapeLogger);
        await Task.Delay(TimeSpan.FromSeconds(2));
        scrapeLogger.Information("Done (MOCK)...");

        ResetUI();
    }

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();
        
        return cts.Token;
    }

    private async void OnScrapeSiteClicked(object? sender, RoutedEventArgs e)
    {
        var cancellationResult = ResetCancellationTokenSource();
        DisableControlsAndClearStatus();

        try
        {
            using var scrapeLogger = GetScrapeLoggerForDisplaySync();
            IPage page = await playwrightService.ConfigurePlaywright(scrapeLogger);

            var imagePage = new ImagePage(page, scrapeConfiguration, tagsToIgnoreCompletely, tagsTextToIgnore, scrapedTagRepository);
            var imagePageService = new ImagePageService(imagePage, fileDetailRepository, fileClassificationService, scrapeConfiguration, scrapeLogger);

            await cancellationResult.Match(
                onSuccess: async ct => await RunScrapeWorkflowAsync(scrapeLogger, page, imagePageService, ct),
                onFailure: ex => 
                {
                    logger.Error(ex, "Scrape failed");

                    return Task.FromException(ex);
                }
            );

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
            ResetUI();
        }
    }

    private async Task RunScrapeWorkflowAsync(Logger scrapeLogger, IPage page, ImagePageService imagePageService, CancellationToken ct = default)
    {
        await RunSearchAsync(scrapeLogger, page, imagePageService, ct);
        await RunSubscriptionsAsync(scrapeLogger, page, imagePageService, ct);
        await RunTopWallpapersAsync(scrapeLogger, page, imagePageService, ct);
    }

    private Logger GetScrapeLoggerForDisplaySync()
    {
        return new LoggerConfiguration()
                        .WriteTo.Logger(logger)
                        .WriteTo.Sink(new StatusLogSink(UpdateStatus))
                        .MinimumLevel.Information()
                        .CreateLogger();
    }

    private void ResetUI()
    {
        EditConfigurationButton.IsEnabled = true;
        ScrapeSiteButton.IsEnabled = true;
        CancelButton.IsEnabled = false;
        cts?.Dispose();
        cts = null;
    }

    private void DisableControlsAndClearStatus()
    {
        EditConfigurationButton.IsEnabled = false;
        ScrapeSiteButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Text = string.Empty;
    }

    private async Task RunTopWallpapersAsync(Logger logger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var topWallpapersPage = new TopWallpapersPage(page, scrapeConfiguration.SearchConfiguration);
        var topWallpapersWorkflow = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration.SearchConfiguration, configurationSaver, logger);
        await topWallpapersWorkflow.RunAsync(ct);
    }

    private async Task RunSubscriptionsAsync(Logger logger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var subscriptionsPage = new SubscriptionsImagesListPage(page, scrapeConfiguration.SearchConfiguration);
        var subscriptionsWorkflow = new SubscriptionsWorkflow(subscriptionsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, logger);
        await subscriptionsWorkflow.RunAsync(ct);
    }

    private async Task RunSearchAsync(Logger logger, IPage page, ImagePageService imagePageService, CancellationToken ct)
    {
        var searchResultsPage = new SearchResultsPage(page, logger);
        var searchWorkflow = new SearchWorkflow(searchResultsPage, imagePageService, scrapeConfiguration.SearchConfiguration, scrapeConfiguration.ScrapeDirectories, configurationSaver, logger);
        await searchWorkflow.RunAsync(ct);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => cts?.Cancel();

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.Post(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });
}
