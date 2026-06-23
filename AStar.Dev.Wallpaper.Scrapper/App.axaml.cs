using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using ScrapeConfigModel = AStar.Dev.Wallpaper.Scrapper.Models.ScrapeConfiguration;

namespace AStar.Dev.Wallpaper.Scrapper;

public partial class App : Application
{
    private IHost _host = null!;

    public static new App Current => (App)Application.Current!;
    public IServiceProvider Services => _host.Services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = ApplicationMetadata.ApplicationFolder
        });

        builder.Configuration.AddUserSecrets<App>(optional: true, reloadOnChange: true);

        builder.Services
            .Configure<ScrapeConfigModel>(builder.Configuration.GetSection(nameof(ScrapeConfigModel)))
            .AddSingleton<ScrapeConfigModel>(sp => sp.GetRequiredService<IDbContextFactory<FilesContext>>()
                .CreateDbContext().ScrapeConfiguration
                .Include(e => e.ConnectionStrings)
                .Include(e => e.UserConfiguration)
                .Include(e => e.SearchConfiguration).ThenInclude(s => s.SearchCategories)
                .Include(e => e.ScrapeDirectories)
                .First()
                .ToAppModel())
            .AddSingleton(sp => new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(sp.GetRequiredService<IConfiguration>())
                .CreateLogger())
            .AddDbContextFactory<FilesContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")))
            .AddTransient<DatabaseInitializationService>()
            .AddTransient<ScrapeConfigurationViewModel>()
            .AddTransient<ScrapeConfigurationView>()
            .AddTransient<Func<ScrapeConfigurationView>>(sp => () => sp.GetRequiredService<ScrapeConfigurationView>())
            .AddTransient<MainWindow>();

        _host = builder.Build();

        Task.Run(async () =>
            await _host.Services.GetRequiredService<DatabaseInitializationService>().InitialiseAsync()
        ).GetAwaiter().GetResult();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnExit;
        }

        _host.Start();
        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        => _host.StopAsync().GetAwaiter().GetResult();
}
