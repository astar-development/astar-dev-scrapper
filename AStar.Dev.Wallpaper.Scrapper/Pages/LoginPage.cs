using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class LoginPage(IPage page, SearchConfiguration searchConfiguration)
{
    private static readonly Random Rng = Random.Shared;

    private readonly SearchConfiguration searchConfiguration = searchConfiguration ?? throw new ArgumentNullException();

    private ILocator Username => page.GetByPlaceholder("Username or email");

    private ILocator Password => page.GetByPlaceholder("Password");

    private ILocator Login => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = " Login", });

    public async Task GoToLoginPageAsync()
    {
        _ = await page.GotoAsync(searchConfiguration.BaseUrl + "/login");
        await SimulateIdleMouseAsync();
    }

    public async Task LoginAsync(string username, string password)
    {
        await Username.ClickAsync();
        await TypeHumanAsync(username);
        await Task.Delay(Rng.Next(300, 700));
        await Password.ClickAsync();
        await TypeHumanAsync(password);
        await Task.Delay(Rng.Next(400, 900));
        await Login.ClickAsync();
    }

    public Task ConfirmLoggedInAsync(string loggedInUrl) // await page.Keyboard.PressAsync("F11"); // should work but doesn't seem to (DownAsync too)
    {
        if(page.Url != loggedInUrl) throw new InvalidOperationException($"Login failed. Expected URL '{loggedInUrl}' but got '{page.Url}'.");

        return Task.CompletedTask;
    }

    private async Task TypeHumanAsync(string text)
    {
        foreach(var ch in text)
        {
            await page.Keyboard.TypeAsync(ch.ToString());
            await Task.Delay(Rng.Next(60, 180));
        }
    }

    private async Task SimulateIdleMouseAsync()
    {
        var viewport = page.ViewportSize;
        if(viewport is null) return;

        for(var i = 0; i < 4; i++)
        {
            var x = Rng.Next(200, viewport.Width  - 200);
            var y = Rng.Next(200, viewport.Height - 200);
            await page.Mouse.MoveAsync(x, y);
            await Task.Delay(Rng.Next(200, 500));
        }
    }
}
