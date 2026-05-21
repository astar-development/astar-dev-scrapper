using System.Reflection;
using AStar.Dev.Wallpaper.Scrapper.ApiClient;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Xunit;

namespace AStar.Dev.Wallpaper.Scrapper.Tests;

public abstract class WallpaperScrapperTestBase : IAsyncLifetime
{
    protected ScrapeConfiguration ScrapeConfig       { get; private set; } = null!;
    protected Logger              Logger             { get; private set; } = null!;
    protected WallhavenApiClient  ApiClient          { get; private set; } = null!;
    protected ConfigurationSaver  ConfigurationSaver { get; private set; } = null!;

    public Task InitializeAsync()
    {
        ScrapeConfig = ConfigurationFactory.Configuration();
        var logging  = ConfigurationFactory.Logging();

        IConfigurationRoot serilogConfig = new ConfigurationBuilder()
                                          .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
                                          .AddJsonFile("appsettings.json")
                                          .Build();

        Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(serilogConfig)
                .CreateLogger();

        ConfigurationSaver = new ConfigurationSaver(ScrapeConfig, logging, Logger);
        ApiClient          = new WallhavenApiClient(ScrapeConfig.SearchConfiguration.ApiKey);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        ConfigurationSaver.SaveUpdatedConfiguration();
        Logger.Information("Shutting down...");
        ApiClient.Dispose();

        return Task.CompletedTask;
    }

    protected ImagePageService CreateImagePageService()
    {
        var tagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely();
        var tagsTextToIgnore       = TagsFactory.LoadTagsTextToIgnore();

        var imagePage = new ImagePage(
            ScrapeConfig.SearchConfiguration,
            ScrapeConfig.ScrapeDirectories,
            ScrapeConfig.ConnectionStrings,
            tagsToIgnoreCompletely,
            tagsTextToIgnore,
            Logger);

        return new ImagePageService(imagePage, Logger);
    }
}
