using System.Reflection;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;

namespace AStar.Dev.Wallpaper.Scrapper.Hooks;

[Binding]
public sealed class Hooks
{
    private readonly IBrowser            browser;
    private readonly ObjectContainer     container;
    private readonly Logger              logger;
    private readonly ScrapeConfiguration scrapeConfiguration;

    public Hooks(ObjectContainer container)
    {
        this.container      = container;
        scrapeConfiguration = ConfigurationFactory.Configuration();
        IPlaywright playwright = Playwright.CreateAsync().Result;

        browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                                                  {
                                                      Headless = scrapeConfiguration.SearchConfiguration.UseHeadless,
                                                      SlowMo   = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
                                                      Channel  = "msedge",

                                                      // Args = new[] { "--start-fullscreen" }
                                                  }).Result;

        IConfigurationRoot configuration = new ConfigurationBuilder()
                                          .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
                                          .AddJsonFile("appSettings.json")
                                          .Build();

        logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

        var logging = new Logging();
        configuration.GetSection("Logging").Bind(logging);

        container.RegisterInstanceAs(logger);
        container.RegisterInstanceAs(logging);
        container.RegisterInstanceAs(scrapeConfiguration);
        container.RegisterInstanceAs(scrapeConfiguration.SearchConfiguration);
        container.RegisterInstanceAs(scrapeConfiguration.ScrapeDirectories);
        container.RegisterInstanceAs(scrapeConfiguration.UserConfiguration);
        container.RegisterInstanceAs(scrapeConfiguration.ConnectionStrings);
        _ = container.RegisterFactoryAs(TagsFactory.LoadTagsToIgnoreCompletely);
        _ = container.RegisterFactoryAs(TagsFactory.LoadTagsTextToIgnore);
    }

    public IPage User { get; private set; } = null!;

    [BeforeScenario]
    public async Task RegisterSingleInstancePractitioner()
    {
        IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = scrapeConfiguration.SearchConfiguration.BaseUrl, ViewportSize = new ViewportSize { Width = 1920, Height = 1080, }, });

        User = await context.NewPageAsync();
        User.SetDefaultTimeout(60_000);
        container.RegisterInstanceAs(context);
        container.RegisterInstanceAs(User);
    }

    [AfterScenario]
    public void AfterScenario()
        => logger.Information("Shutting down...");
}
