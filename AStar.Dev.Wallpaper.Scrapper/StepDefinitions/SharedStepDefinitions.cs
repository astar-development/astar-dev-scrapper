using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Reqnroll;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.StepDefinitions;

[Binding]
public sealed class SharedStepDefinitions(LoginPage loginPage, UserConfiguration userConfiguration, SearchConfiguration searchConfiguration, ConfigurationSaver configurationSaver, Logger logger)
    : IDisposable
{
    private readonly ConfigurationSaver  configurationSaver  = configurationSaver  ?? throw new ArgumentNullException();
    private readonly LoginPage           loginPage           = loginPage           ?? throw new ArgumentNullException();
    private readonly SearchConfiguration searchConfiguration = searchConfiguration ?? throw new ArgumentNullException();
    private readonly UserConfiguration   userConfiguration   = userConfiguration   ?? throw new ArgumentNullException();
    private          bool                disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Given("I access the website")]
    public async Task GivenIAccessTheWebsite()
    {
        logger.Debug("Going to the login page...");
        await loginPage.GoToLoginPageAsync();
    }

    [When("I enter valid credentials")]
    public async Task WhenIEnterValidCredentials()
        => await loginPage.LoginAsync(userConfiguration.Username, userConfiguration.Password);

    [Then("I can see that I have logged in successfully")]
    public async Task ThenICanSeeThatIHaveLoggedInSuccessfully()
        => await loginPage.ConfirmLoggedInAsync($"{searchConfiguration.BaseUrl}/user/{userConfiguration.Username}");

    protected virtual void Dispose(bool disposing)
    {
        if(disposed) return;

        if(disposing) configurationSaver.SaveUpdatedConfiguration();

        disposed = true;
    }
}
