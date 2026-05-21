using System.Reflection;
using Xunit;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

namespace AStar.Dev.Wallpaper.Scrapper.Tests;

public sealed class ScrapperTests : IAsyncLifetime
{
    private ScrapeConfiguration _config         = null!;
    private Logger              _logger         = null!;
    private IPlaywright         _playwright     = null!;
    private IBrowser            _browser        = null!;
    private IBrowserContext     _context        = null!;
    private IPage               _page           = null!;
    private ConfigurationSaver  _configSaver    = null!;
    private ImagePageService    _imagePageSvc   = null!;

    public async Task InitializeAsync()
    {
        _config = ConfigurationFactory.Configuration();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appSettings.json", optional: true)
            .Build();

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .Enrich.WithExceptionDetails()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var logging = new Logging();
        configuration.GetSection("Logging").Bind(logging);
        _configSaver = new ConfigurationSaver(_config, logging, _logger);

        _playwright = await Playwright.CreateAsync();
        _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _config.SearchConfiguration.UseHeadless,
            SlowMo   = _config.SearchConfiguration.SlowMotionDelay,
            Channel  = "msedge",
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL      = _config.SearchConfiguration.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
        });

        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(60_000);

        var tagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely();
        var tagsTextToIgnore       = TagsFactory.LoadTagsTextToIgnore();

        var imagePage = new ImagePage(
            _page,
            _config.SearchConfiguration,
            _config.ScrapeDirectories,
            _config.ConnectionStrings,
            tagsToIgnoreCompletely,
            tagsTextToIgnore,
            _logger);

        _imagePageSvc = new ImagePageService(imagePage, _logger);

        var loginPage = new LoginPage(_page, _config.SearchConfiguration);
        await loginPage.GoToLoginPageAsync();
        await loginPage.LoginAsync(_config.UserConfiguration.Username, _config.UserConfiguration.Password);
        await loginPage.ConfirmLoggedInAsync($"{_config.SearchConfiguration.BaseUrl}/user/{_config.UserConfiguration.Username}");
    }

    public async Task DisposeAsync()
    {
        _configSaver.SaveUpdatedConfiguration();
        _logger.Information("Shutting down...");
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task DownloadImagesFromSearchResults()
    {
        var searchResultsPage = new SearchResultsPage(_page, _logger);
        var workflow          = new SearchWorkflow(searchResultsPage, _imagePageSvc, _config.SearchConfiguration, _config.ScrapeDirectories, _configSaver, _logger);
        await workflow.RunAsync();
    }

    [Fact]
    public async Task DownloadSubscriptionImages()
    {
        var subscriptionsPage = new SubscriptionsImagesListPage(_page, _config.SearchConfiguration);
        var workflow          = new SubscriptionsWorkflow(subscriptionsPage, _imagePageSvc, _config.SearchConfiguration, _config.ScrapeDirectories, _configSaver, _logger);
        await workflow.RunAsync();
    }

    [Fact]
    public async Task DownloadTopWallpapers()
    {
        var topWallpapersPage = new TopWallpapersPage(_page, _config.SearchConfiguration);
        var workflow          = new TopWallpapersWorkflow(topWallpapersPage, _imagePageSvc, _config.SearchConfiguration, _configSaver, _logger);
        await workflow.RunAsync();
    }
}
