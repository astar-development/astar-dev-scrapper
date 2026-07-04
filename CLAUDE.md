# CLAUDE.md

Guidance for Claude Code when working in this repo.

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

## NON-NEGOTIABLE RULES

ALWAYS use serena / graphify to aid understanding / you need to explore the code-base. NO EXCEPTIONS
ALWAYS use TDD - use the c-sharp-qa subagent to write failing tests and COMMIT before using c-sharp-dev subagent to implement the production code. Ensure all tests (new and existing) pass before reporting success.
ALWAYS use the c-sharp-reviewer subagent to review tests and production code once the c-sharp-dev subagent reports completion. ALWAYS fix the issues reported - use a new c-sharp-dev subagent to fix.

## LESSONS LEARNED — DO NOT REPEAT THESE MISTAKES

### Trust the user over code inference
When the user states a runtime fact (e.g. "the database is X"), accept it. Do not override it with inferences from file timestamps, file sizes, or indirect code paths. The user knows their own secrets and runtime environment.

### "REDACTED!" is a placeholder — never infer the real value
Any value in `appsettings.json` (or any readable config file) that reads `"REDACTED!"`, `"<secret>"`, or similar is a placeholder. The real value is in user secrets. Never attempt to infer what the real value might be from circumstantial evidence. If the value matters, ask the user.

### Runtime SQLite connection string
The runtime DB path is set via user secrets `ConnectionStrings:Sqlite`. It resolves to `Data Source=/home/jbarden/Documents/Scrapper/files.db`. This is the one and only runtime database. `FilesDb.db` is NOT the runtime database.

### Fix only what was asked — do not change scope
Read the request literally. "Make the exports work" means fix the bug that causes empty output — it does NOT mean restructure buttons, merge handlers, or change UI layout. If in doubt, ask before changing anything beyond the stated scope.

### Stop immediately when told to stop
If the user says "DO NOT CHANGE ANYTHING", stop all tool calls immediately. Explain in text only. Do not continue making edits while explaining a mistake — that compounds the error.

## Architecture

Targets **net10.0** across all projects. `TreatWarningsAsErrors` enabled in Debug and Release.

### Projects

**`AStar.Dev.Wallpaper.Scrapper`** — main executable. Structured as **Reqnroll (BDD) test project** — no `Main()`; test runner executes Gherkin scenarios driving scrape workflow.

- `Features/` — Gherkin `.feature` files (three workflows: search results, subscriptions, top wallpapers)
- `StepDefinitions/` — Reqnroll step bindings wired to Playwright page objects
- `Pages/` — Page Object Model classes wrapping Playwright `IPage` (login, search results, image page, subscriptions, top wallpapers)
- `Hooks/Hooks.cs` — Reqnroll lifecycle: spins up Playwright Chromium/Edge, builds Serilog logger, registers DI into Reqnroll's `ObjectContainer`
- `Support/` — helpers: `ConfigurationFactory`, `ConfigurationSaver`, `DirectoryHelper`, `ImageRetrieverHelper`, `ImageSaveHelper`, `TagsFactory`
- `Models/` — strongly-typed bindings for `appsettings.json` sections (`ScrapeConfiguration`, `SearchConfiguration`, `ScrapeDirectories`, `UserConfiguration`, etc.)
- `DTOs/` — deserialization models for `tagsTextToIgnore.json` and `tagsToIgnoreCompletely.json`

**`AStar.Dev.Guard.Clauses`** — NuGet package. Single `GuardAgainst.Null<T>()` helper.

**`AStar.Dev.Infrastructure.FilesDb`** — NuGet package. EF Core (SQL Server) infrastructure for file metadata. `EnumerableExtensions` provides filtering/sorting/duplicate-detection over `FileDetail` collections. Marked `[Refactor]` — may duplicate logic in files API project.

**`AStar.Dev.Utilities`** — NuGet package. Shared extension methods/helpers (`StringExtensions`, `LinqExtensions`, `EncryptionExtensions`, `EnumExtensions`, `PathOperationExtensions`, `RegexExtensions`, `ObjectExtensions`, `ApplicationPathsProvider`).

### Configuration

`ConfigurationFactory` merges two sources (later overrides earlier):
1. `appsettings.json` in project root — non-secret defaults
2. **User Secrets** (`UserSecretsId: c35e09dc-dc30-416a-95a6-ec1a5ba1b43f`) — SQL connection string, login credentials

Sensitive fields (`connectionStrings.sqlServer`, `userConfiguration.password`) must live in User Secrets, not `appsettings.json`.

`ConfigurationFactory` also normalises `SearchConfiguration.SearchString` and `Subscriptions` URLs (strips base URL prefix and trailing page number) before returning.

### Logging

Serilog via `appsettings.json`. Writes to Console and Seq (`http://localhost:5341`). `Hooks` constructor also hard-codes Seq sink at same address.

### Tag filtering

Two JSON files control skipped wallpapers:
- `tagsToIgnoreCompletely.json` — skip images whose tags include these values
- `tagsTextToIgnore.json` — skip on partial text match in tag names

Both loaded by `TagsFactory`, registered into Reqnroll DI in `Hooks`.

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- For cross-module "how does X relate to Y" questions, prefer `graphify query "<question>"`, `graphify path "<A>" "<B>"`, or `graphify explain "<concept>"` over grep — these traverse the graph's EXTRACTED + INFERRED edges instead of scanning files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
