

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build AStar.Dev.Web.Scrapper.slnx

# Run all Reqnroll scenarios
dotnet test AStar.Dev.Web.Scrapper.slnx

# Run a specific scenario by name
dotnet test --filter "FullyQualifiedName~DownloadImagesFromSearchResults"

# Run a specific project's tests
dotnet test AStar.Dev.Wallpaper.Scrapper/AStar.Dev.Wallpaper.Scrapper.csproj
```

## Architecture

Solution targets **net10.0** across all projects. `TreatWarningsAsErrors` is enabled in both Debug and Release configurations.

### Projects

**`AStar.Dev.Wallpaper.Scrapper`** — the main executable. Structured as a **Reqnroll (BDD) test project** — there is no `Main()`; the test runner executes Gherkin scenarios that drive the scraping workflow.

- `Features/` — Gherkin `.feature` files (three scraping workflows: search results, subscriptions, top wallpapers)
- `StepDefinitions/` — Reqnroll step bindings wired to Playwright page objects
- `Pages/` — Page Object Model classes wrapping Playwright `IPage` (login, search results, image page, subscriptions, top wallpapers)
- `Hooks/Hooks.cs` — Reqnroll lifecycle: spins up Playwright Chromium/Edge browser, builds Serilog logger, registers all DI instances into Reqnroll's `ObjectContainer`
- `Support/` — helpers: `ConfigurationFactory` (loads config), `ConfigurationSaver`, `DirectoryHelper`, `ImageRetrieverHelper`, `ImageSaveHelper`, `TagsFactory`
- `Models/` — strongly-typed bindings for `appsettings.json` sections (`ScrapeConfiguration`, `SearchConfiguration`, `ScrapeDirectories`, `UserConfiguration`, etc.)
- `DTOs/` — deserialization models for `tagsTextToIgnore.json` and `tagsToIgnoreCompletely.json`

**`AStar.Dev.Guard.Clauses`** — NuGet package. Single `GuardAgainst.Null<T>()` helper.

**`AStar.Dev.Infrastructure.FilesDb`** — NuGet package. EF Core (SQL Server) infrastructure for file metadata. `EnumerableExtensions` provides filtering/sorting/duplicate-detection over `FileDetail` collections. Note: marked with `[Refactor]` — may duplicate logic in the files API project.

**`AStar.Dev.Utilities`** — NuGet package. Extension methods and helpers shared across AStar packages (`StringExtensions`, `LinqExtensions`, `EncryptionExtensions`, `EnumExtensions`, `PathOperationExtensions`, `RegexExtensions`, `ObjectExtensions`, `ApplicationPathsProvider`).

### Configuration

`ConfigurationFactory` merges two sources (in order, later overrides earlier):
1. `appsettings.json` in the project root — non-secret defaults
2. **User Secrets** (`UserSecretsId: c35e09dc-dc30-416a-95a6-ec1a5ba1b43f`) — SQL connection string, login credentials

Sensitive fields (`connectionStrings.sqlServer`, `userConfiguration.password`) must live in User Secrets, not `appsettings.json`.

`ConfigurationFactory` also normalises `SearchConfiguration.SearchString` and `Subscriptions` URLs (strips base URL prefix and trailing page number) before returning.

### Logging

Serilog, configured via `appsettings.json`. Writes to Console and Seq (`http://localhost:5341`). The `Hooks` constructor also hard-codes a Seq sink at the same address.

### Tag filtering

Two JSON files control which wallpapers are skipped:
- `tagsToIgnoreCompletely.json` — skip any image whose tags include these values
- `tagsTextToIgnore.json` — skip based on partial text match in tag names

Both are loaded by `TagsFactory` and registered into Reqnroll's DI container in `Hooks`.
