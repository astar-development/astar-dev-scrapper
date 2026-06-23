using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;

public partial class ScrapeConfigurationView : Window
{
    public ScrapeConfigurationView(ScrapeConfigurationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        await ((ScrapeConfigurationViewModel)DataContext!).LoadAsync();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if(DataContext is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
