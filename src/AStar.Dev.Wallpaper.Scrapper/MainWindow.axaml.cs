using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.ConfigurationImportExport;
using AStar.Dev.Wallpaper.Scrapper.Tags;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using AStar.Dev.Wallpaper.Scrapper.Dialogs;
using Serilog;
using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window, IDisposable
{
    private readonly Func<ScrapeConfigurationView> scrapeConfigViewFactory;
    private readonly Func<ClassificationsView> classificationsViewFactory;
    private readonly Func<ConfigurationImportExportView> configurationImportExportViewFactory;
    private readonly Func<TagsView> tagsViewFactory;
    private readonly ILogger logger;
    private readonly SearchWorkflowFunctional searchWorkflowFunctional;
    private readonly LogBroadcaster logBroadcaster;
    private readonly IDatabaseResetService databaseResetService;
    private CancellationTokenSource? cts;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory, Func<ClassificationsView> classificationsViewFactory, Func<ConfigurationImportExportView> configurationImportExportViewFactory, Func<TagsView> tagsViewFactory, SearchWorkflowFunctional searchWorkflowFunctional, ILogger logger, LogBroadcaster logBroadcaster, IDatabaseResetService databaseResetService)
    {
        this.scrapeConfigViewFactory = scrapeConfigViewFactory;
        this.classificationsViewFactory = classificationsViewFactory;
        this.configurationImportExportViewFactory = configurationImportExportViewFactory;
        this.tagsViewFactory = tagsViewFactory;
        this.logger = logger;
        this.searchWorkflowFunctional = searchWorkflowFunctional;
        this.logBroadcaster = logBroadcaster;
        this.databaseResetService = databaseResetService;
        logBroadcaster.MessageLogged += UpdateStatus;
        InitializeComponent();
        Closed += (_, _) => cts?.Dispose();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await scrapeConfigViewFactory().ShowDialog(this);

    private async void OnEditClassificationsClicked(object? sender, RoutedEventArgs e)
        => await classificationsViewFactory().ShowDialog(this);

    private async void OnExportImportConfigurationClicked(object? sender, RoutedEventArgs e)
        => await configurationImportExportViewFactory().ShowDialog(this);

    private async void OnEditTagsClicked(object? sender, RoutedEventArgs e)
        => await tagsViewFactory().ShowDialog(this);

    private async void OnResetDatabaseClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmationDialog("This will delete all downloaded files and reset search category progress. Continue?");
        var confirmed = await dialog.ShowDialog<bool>(this);

        if (!confirmed)
            return;

        try
        {
            await databaseResetService.ResetAsync();
            UpdateStatus("Database reset completed successfully.");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Database reset failed");
            UpdateStatus($"Database reset failed: {ex.Message}");
        }
    }

    private async void OnScrapeSiteFunctionalClicked(object? sender, RoutedEventArgs e)
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
            .Tap(_ => logger.Information("Configuring Playwright..."))
            .Tap(_ => logger.Information("Starting scrape..."))
            .BindAsync(page => searchWorkflowFunctional.RunAsync(logger, cts!.Token))
            .TapAsync(_ => logger.Information("Scrape completed..."))
            .EnsureAsync(() => ResetUI());

    private Result<CancellationToken, Exception> ResetCancellationTokenSource()
    {
        cts = new CancellationTokenSource();

        return cts.Token;
    }

    private Result<CancellationToken, string> DisableControlsAndClearStatus(CancellationToken ct = default)
    {
        ScrapeSiteNewButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        StatusLabel.Text = string.Empty;

        return ct;
    }

    private void ResetUI()
        => Dispatcher.UIThread.InvokeAsync(() =>
            {
                ScrapeSiteNewButton.IsEnabled = true;
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

    public void Dispose()
    {
        logBroadcaster.MessageLogged -= UpdateStatus;
        cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
