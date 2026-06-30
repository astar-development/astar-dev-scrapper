using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.Tags;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using AStar.Dev.Wallpaper.Scrapper.Dialogs;
using Serilog;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window, IDisposable
{
    private readonly Func<ScrapeConfigurationView> scrapeConfigViewFactory;
    private readonly Func<ClassificationsView> classificationsViewFactory;
    private readonly Func<TagsView> tagsViewFactory;
    private readonly ILogger logger;
    private readonly SearchWorkflowFunctional searchWorkflowFunctional;
    private readonly LogBroadcaster logBroadcaster;
    private readonly IDatabaseResetService databaseResetService;
    private CancellationTokenSource? cts;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory, Func<ClassificationsView> classificationsViewFactory, Func<TagsView> tagsViewFactory, SearchWorkflowFunctional searchWorkflowFunctional, ILogger logger, LogBroadcaster logBroadcaster, IDatabaseResetService databaseResetService)
    {
        this.scrapeConfigViewFactory = scrapeConfigViewFactory;
        this.classificationsViewFactory = classificationsViewFactory;
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

    private async void OnEditTagsClicked(object? sender, RoutedEventArgs e)
        => await tagsViewFactory().ShowDialog(this);

    private async void OnResetDatabaseClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dbDialog = new ConfirmationDialog("This will reset search category progress and delete all file records. Continue?");
            var dbConfirmed = await dbDialog.ShowDialog<bool>(this);

            if (!dbConfirmed)
                return;

            try
            {
                await databaseResetService.ResetAsync(CancellationToken.None);
                UpdateStatus("Database reset completed successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Database reset failed");
                UpdateStatus($"Database reset failed: {ex.Message}");
                return;
            }

            var fileDialog = new ConfirmationDialog("This will permanently delete all downloaded files from the save directory. Continue?");
            var fileConfirmed = await fileDialog.ShowDialog<bool>(this);

            if (!fileConfirmed)
                return;

            try
            {
                await databaseResetService.DeleteSaveDirectoryAsync(CancellationToken.None);
                UpdateStatus("Save directory deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Save directory deletion failed");
                UpdateStatus($"Save directory deletion failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error during database reset");
            UpdateStatus($"Unexpected error: {ex.Message}");
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
                cts = new();
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
