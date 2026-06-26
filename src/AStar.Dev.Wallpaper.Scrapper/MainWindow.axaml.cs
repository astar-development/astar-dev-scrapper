using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using Serilog;
using Serilog.Core;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Workflows;

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
    private readonly SearchWorkflowFunctional imagePageServiceFunctional;
    private readonly SearchWorkflowFunctional searchWorkflowFunctional;
    private CancellationTokenSource? cts;
    private readonly Logger scrapeLogger;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory,SearchWorkflowFunctional imagePageServiceFunctional, IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, SearchWorkflowFunctional searchWorkflowFunctional, Logger logger, IScrapedTagRepository scrapedTagRepository, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ConfigurationSaver configurationSaver, TagsToIgnoreCompletely tagsToIgnoreCompletely, TagsTextToIgnore tagsTextToIgnore)
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
        this.imagePageServiceFunctional = imagePageServiceFunctional;
        this.searchWorkflowFunctional = searchWorkflowFunctional;
        this.scrapeLogger = GetScrapeLoggerForDisplaySync();
        InitializeComponent();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await scrapeConfigViewFactory().ShowDialog(this);

    private async void OnScrapeSiteFunctionalClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    scrapeLogger.Error(ex, "Failed to reset cancellation token source");
                    UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => scrapeLogger.Information("Configuring Playwright..."))
            .Tap(_ => scrapeLogger.Information("Starting scrape..."))
            .BindAsync(page => searchWorkflowFunctional.RunAsync(cts!.Token))
            .TapAsync(_ => scrapeLogger.Information("Scrape completed..."))
            .EnsureAsync(() => ResetUI());

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    scrapeLogger.Error(ex, "Failed to reset cancellation token source");
                    UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => scrapeLogger.Information("Exporting classifications..."))
            .MapAsync(_ => fileClassificationService.ExportClassificationsAsync(cts!.Token))
            .TapAsync(_ => scrapeLogger.Information("Export completed..."))
            .EnsureAsync(() => ResetUI());

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
    => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    scrapeLogger.Error(ex, "Failed to reset cancellation token source");
                    UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => scrapeLogger.Information("Importing classifications..."))
            .MapAsync(_ => fileClassificationService.ImportClassificationsAsync(cts!.Token))
            .TapAsync(_ => scrapeLogger.Information("Import completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();
        
        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        EditConfigurationButton.IsEnabled = false;
        ScrapeSiteNewButton.IsEnabled = false;
        ExportButton.IsEnabled = false;
        ImportButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Text = string.Empty;
        
        return ct;
    }

    private Logger GetScrapeLoggerForDisplaySync() => new LoggerConfiguration()
                        .WriteTo.Logger(logger)
                        .WriteTo.Sink(new StatusLogSink(UpdateStatus))
                        .MinimumLevel.Information()
                        .CreateLogger();

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                EditConfigurationButton.IsEnabled = true;
                ScrapeSiteNewButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                ImportButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                cts?.Dispose();
                cts = null;
            });

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => cts?.Cancel();

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });
}
