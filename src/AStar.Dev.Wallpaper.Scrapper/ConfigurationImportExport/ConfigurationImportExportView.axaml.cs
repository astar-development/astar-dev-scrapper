using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.FunctionalParadigm;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.ConfigurationImportExport;

public partial class ConfigurationImportExportView : Window, IDisposable
{
    private readonly ScrapeConfigurationService scrapeConfigurationService;
    private readonly IImportExportService importExportService;
    private readonly ILogger logger;
    private readonly LogBroadcaster logBroadcaster;
    private CancellationTokenSource? cts;

    public ConfigurationImportExportView(ScrapeConfigurationService scrapeConfigurationService, IImportExportService importExportService, ILogger logger, LogBroadcaster logBroadcaster)
    {
        this.scrapeConfigurationService = scrapeConfigurationService;
        this.importExportService = importExportService;
        this.logger = logger;
        this.logBroadcaster = logBroadcaster;
        logBroadcaster.MessageLogged += UpdateStatus;
        InitializeComponent();
        Closed += (_, _) => cts?.Dispose();
    }

    private async void OnExportScrapeConfigClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    UpdateStatus($"Error: {ex.Message}");
                    return ex.Message;
                }
            )
            .Tap(_ => logger.Information("Exporting scrape configuration..."))
            .MapAsync(_ => scrapeConfigurationService.ExportScrapeConfigurationAsync(cts!.Token))
            .Tap(importExportService.ExportScrapeConfigurationToFile)
            .TapAsync(_ => logger.Information("Scrape configuration export completed..."))
            .EnsureAsync(() => ResetUI());

    private async void OnImportScrapeConfigClicked(object? sender, RoutedEventArgs e)
        => _ = await ResetCancellationTokenSource()
            .Match<CancellationToken, Exception, Result<CancellationToken, string>>(
                onSuccess: DisableControlsAndClearStatus,
                onFailure: ex =>
                {
                    logger.Error(ex, "Failed to reset cancellation token source");
                    return ex.Message;
                }
            )
            .Tap(_ => logger.Information("Importing scrape configuration..."))
            .Bind(_ => importExportService.ImportScrapeConfigurationFromFile())
            .MapAsync(entity => scrapeConfigurationService.ImportScrapeConfigurationAsync(entity, cts!.Token))
            .TapAsync(_ => logger.Information("Scrape configuration import completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ExportScrapeConfigButton.IsEnabled = false;
        ImportScrapeConfigButton.IsEnabled = false;
        StatusLabel.Text = string.Empty;

        return ct;
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExportScrapeConfigButton.IsEnabled = true;
                ImportScrapeConfigButton.IsEnabled = true;
                cts?.Dispose();
                cts = null;
            });

    private void UpdateStatus(string message)
        => Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text += message + Environment.NewLine;
            StatusScroller.ScrollToEnd();
        });

    public void Dispose()
    {
        logBroadcaster.MessageLogged -= UpdateStatus;
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
