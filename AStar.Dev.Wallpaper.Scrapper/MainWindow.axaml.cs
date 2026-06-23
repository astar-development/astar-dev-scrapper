using Avalonia.Controls;
using Avalonia.Interactivity;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class MainWindow : Window
{
    private readonly Func<ScrapeConfigurationView> _scrapeConfigViewFactory;

    public MainWindow(Func<ScrapeConfigurationView> scrapeConfigViewFactory)
    {
        _scrapeConfigViewFactory = scrapeConfigViewFactory;
        InitializeComponent();
    }

    private async void OnEditConfigurationClicked(object? sender, RoutedEventArgs e)
        => await _scrapeConfigViewFactory().ShowDialog(this);
}
