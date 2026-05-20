using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Playwright;
using Shouldly;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public class LoginPage(IPage page, SearchConfiguration searchConfiguration)
{
    private readonly SearchConfiguration searchConfiguration = searchConfiguration ?? throw new ArgumentNullException();

    private ILocator Username => page.GetByPlaceholder("Username or email");

    private ILocator Password => page.GetByPlaceholder("Password");

    private ILocator Login => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = " Login", });

    public async Task GoToLoginPageAsync()
        => _ = await page.GotoAsync(searchConfiguration.LoginUrl);

    public async Task LoginAsync(string username, string password)
    {
        await Username.FillAsync(username);
        await Password.FillAsync(password);
        await Login.ClickAsync();
    }

    internal Task ConfirmLoggedInAsync(string loggedInUrl) // await page.Keyboard.PressAsync("F11"); // should work but doesn't seem to (DownAsync too)
    {
        page.Url.ShouldBe(loggedInUrl);

        return Task.CompletedTask;
    }
}
