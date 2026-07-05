# AStar.Dev.Wallpaper.Scrapper — Functional Simplification Plan

**Status:** Proposed (no code changes made yet)
**Date:** 2026-07-04
**Scope:** `src/AStar.Dev.Wallpaper.Scrapper` + additions to `src/AStar.Dev.FunctionalParadigm`

---

## 1. Goals

1. Simplify `AStar.Dev.Wallpaper.Scrapper` — remove duplicate/dead code, shrink classes, one obvious path per behaviour.
2. Adopt `AStar.Dev.FunctionalParadigm` throughout: `Result<T,E>` pipelines, plus new `Exceptional<T>`, `Validation<T>`, `Try`, and `Pipe`/`Compose` helpers.
3. Make code pure wherever possible: extract decision logic into static, side-effect-free functions; inject side effects (`TimeProvider`, delay providers, file system, random) as dependencies or function parameters so the core is deterministic and unit-testable.
4. Replace `try/catch → log → rethrow` blocks and boolean/null signalling with typed `Result` flows using a domain error DU.

## 2. Decisions already made (with Jason, 2026-07-04)

| Decision | Choice |
|---|---|
| Dead/duplicate classes | Delete true duplicates only. **Keep** `SubscriptionsWorkflow` and `TopWallpapersWorkflow` (will be re-wired to UI) and refactor them too. |
| New FunctionalParadigm types | Add **all four**: `Exceptional<T>`, `Validation<T>`, `Try`/`Try.RunAsync`, `Pipe`/`Compose` helpers. |
| Error model | **Domain error DU** (`ScrapeError`) carried in `Result<T, ScrapeError>`; exceptions lifted via an `UnexpectedError(Exception)` case. |
| Latent bugs | **Fix-first Phase 0** (TDD, committed before any refactor). |
| DB filename (`App.axaml.cs:71`) | Confirmed legacy copy-paste. New path: **`/home/jbarden/Documents/Scrapper/scrapper.db`**. Fixed in Phase 0 (B6). |
| DB data migration | **Start clean** — fresh database at the new path; data re-imported manually via the existing Import buttons. No file copy/migration code. |
| DB path source | **Configurable** — read from configuration/user secrets (`ConnectionStrings:Sqlite`) with `/home/jbarden/Documents/Scrapper/scrapper.db` as the default; no hard-coded path in `App.axaml.cs`. |
| Subscriptions/TopWallpapers UI wiring | **Separate piece of work** — out of scope for this plan. Phase 5 delivers compiled, DI-registered, fully-tested workflows only. |
| Page-loop delay | **Unify on configuration** (`ImagePauseInSeconds`) — the hard-coded 2s delay in `SearchWorkflow.ProcessAllCategoryPagesAsync` goes; `PagedScrapeRunner` reads the configured delay. |
| Subscription header parsing (`IndexOf("New")` logic) | Written long ago but **assumed still correct** — Phase 3 pure-parser extraction freezes current behaviour into tests as-is. |

## 3. Current-state findings

### 3.1 The live execution path

`MainWindow.OnScrapeSiteFunctionalClicked` → `SearchWorkflowFunctional` → `SearchResultsPageFunctional` + `ImagePageService` → `ImagePage` → `FileClassificationService` / `FileDetailRepository` / `ScrapedTagRepository`, with `ConfigurationSaver` (the *non*-Functional one) persisting progress.

### 3.2 Duplicates and dead code

| File | Verdict |
|---|---|
| `Support/ConfigurationSaverFunctional.cs` | Byte-identical copy of `ConfigurationSaver`. Registered in DI, only used by dead `TopWallpapersWorkflowFunctional`. **Delete.** |
| `Pages/SearchResultsPage.cs` | Byte-identical to `SearchResultsPageFunctional`. Not registered in DI. **Delete** (keep one class, rename — see Phase 2). |
| `Workflows/SearchWorkflow.cs` | Superseded twin of `SearchWorkflowFunctional`. Not registered. **Delete.** |
| `Workflows/TopWallpapersWorkflowFunctional.cs` | Near-identical to `TopWallpapersWorkflow` (only saver type differs). **Delete**; keep `TopWallpapersWorkflow` per decision. |
| `Pages/TopWallpapersPageFunctional.cs` | Duplicate of `TopWallpapersPage` with the `page ??=` init fixed. **Merge**: keep one class carrying the fix. |
| `Services/ImagePageServiceFunctional.cs` | ~70 of 100 lines are commented-out code; live part loads config it never uses and wraps `SearchWorkflowFunctional` adding nothing. **Delete.** |
| `Pages/ImagePageResultFunctional.cs` | Stub — loads config, logs two lines, returns `Unit`. Registered but never resolved. **Delete.** |
| `Support/ConfigurationSaver.cs` line 32 note | Class name kept; "Functional" suffix disappears everywhere (see naming below). |

**Naming rule for the end state:** no `Functional` suffixes. After deletion/merge the survivors are renamed: `SearchWorkflowFunctional` → `SearchWorkflow`, `SearchResultsPageFunctional` → `SearchResultsPage`. The suffix currently marks the *new* implementation, not a different responsibility.

### 3.3 Latent bugs (Phase 0 targets)

| # | Location | Bug |
|---|---|---|
| B1 | `Workflows/SearchWorkflowFunctional.cs:26-27` (same in `SearchWorkflow.cs:26-27`) | `FilterSearchCategories(...)` result assigned to `searchCategories` then **discarded** — the unfiltered list is passed to `ProcessSearchCategoriesAsync`, so the resume-from-last-category optimisation never applies. |
| B2 | `Pages/ImagePage.cs:39-41` | `scrapedTagRepository.SaveAsync` called **twice** with the same data (second call differs only by a redundant `.Select(t => t)`). |
| B3 | `Pages/TopWallpapersPage.cs:17,21` | `LoadTopWallpapersPageAsync` and `PageInfoAsync` use `page` before any `page ??= ConfigurePlaywrightAsync()` — `NullReferenceException` on first call (the `...Functional` twin fixed this; the fix must survive the merge). |
| B4 | `Workflows/SearchWorkflowFunctional.cs:105` | `UpdateSubDirectoryIfRequired` checks `scrapeDirectories is null` but the field is assigned in `RunAsync` before any call — dead branch referencing a 5-arg constructor; collapses to the simple `with` update. |
| B5 | `MainWindow.axaml.cs:116` | `BindAsync(page => searchWorkflowFunctional.RunAsync(logger, cts!.Token))` runs the whole scrape **even when `DisableControlsAndClearStatus` was never called** — the failure lambda of the preceding `Match` returns `ex.Message` as a *success* string, so the chain continues. Failure branch must produce a `Fail`, not a value. |
| B6 | `App.axaml.cs:71` | DB path is `"astar-dev-onedrive-sync".ApplicationDirectory().CombinePath("astar-dev-onedrive-sync.db")` — confirmed legacy copy-paste from another app. Must become `/home/jbarden/Documents/Scrapper/scrapper.db` (configurable, clean database, manual re-import — decisions in §2). |

### 3.4 Structural smells the plan addresses

- **try/catch → log → rethrow** repeated in every page/workflow/saver method (`SearchResultsPage*` ×3, `ConfigurationSaver*`, all four workflow `RunAsync`s). This is exactly what `Exceptional<T>`/`Try` + a single `Tap`-based logging step removes.
- **Mutable record-field churn**: `SearchWorkflowFunctional` holds `searchConfiguration`/`scrapeDirectories` as mutable fields and reassigns them via `with` throughout the loop — state threading disguised as immutability. Replace with an explicit loop-state record passed/returned by pure functions.
- **Hidden side effects in constructors/lazy init**: `page ??= playwrightService.ConfigurePlaywrightAsync()` copies in 10+ methods across 5 page classes. Extract once.
- **Parsing mixed with I/O**: `GetPageInfoAsync` does Playwright I/O *and* header-string parsing (`IndexOf`/`Replace`/`decimal.Parse`) in one method — parsing is pure and should be a static function with its own tests.
- **Boolean/null signalling**: `ImagePageResult.Skip` + `ImageUrl is null`, `IResponse?` with `is { Ok: false }` checks — replace with `Result`/`Option`.
- **Magic numbers**: `24` (images per page) in three classes, `Take(24)`, `TimeSpan.FromSeconds(2)`, retry delay `10`, thumbnail `500`/`20`. Constants/config.
- **`ImagePage.ProcessTheImageTagsAsync`** mixes DB writes, mutation of a `List<string>` parameter, and tag-classification rules — the rules are pure and dense enough to deserve their own type with table-driven tests.
- **`ScrapeConfigurationService.ImportScrapeConfigurationAsync`** / `ScrapeConfigurationViewModel.MapToEntity` — 20-line property-copy walls; source-gen or mapping extensions shrink them (secondary priority).

## 4. New FunctionalParadigm objects (Phase 1)

All new types follow the repo DU convention: abstract base record + case records in the same file + static factory class with `Create`/named factories. Full XML docs, TDD in `AStar.Dev.FunctionalParadigm.Tests.Unit` first.

### 4.1 `Exceptional<T>`

```csharp
public abstract record Exceptional<T>;
public sealed record Success<T>(T Value) : Exceptional<T>;
public sealed record Failure<T>(Exception Exception) : Exceptional<T>;
```

Plus `Match`, `Map`, `Bind`, `Tap`, async overloads mirroring `ResultExtensions`, and `ToResult<T, TError>(Func<Exception, TError> mapError)` to lift into `Result` pipelines.

### 4.2 `Try` / `Try.RunAsync`

```csharp
public static class Try
{
    public static Exceptional<T> Run<T>(Func<T> operation);
    public static Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation);
    // CancellationToken-aware overload: OperationCanceledException always rethrown, never captured.
}
```

This is the bridge that deletes every `try { … } catch (Exception ex) { log; throw; }` block: `await Try.RunAsync(() => page.GotoAsync(url)).Tap(..., ex => logger.Error(...))`.

**Rule baked into tests:** `OperationCanceledException` (incl. `TaskCanceledException`) is never swallowed into a `Failure` — cancellation stays an exception so `ct.ThrowIfCancellationRequested()` semantics survive.

### 4.3 `Validation<T>`

```csharp
public abstract record Validation<T>;
public sealed record Valid<T>(T Value) : Validation<T>;
public sealed record Invalid<T>(IReadOnlyList<ValidationError> Errors) : Validation<T>;
public sealed record ValidationError(string Property, string Message);
```

Applicative: `Apply`/`Combine` accumulate errors instead of stopping at the first; `ToResult(Func<IReadOnlyList<ValidationError>, TError>)` bridges into pipelines. First consumers: `ScrapeConfiguration` validation at startup, import DTO validation in `ImportExportService`, `ScrapeConfigurationViewModel.SaveAsync`.

### 4.4 `Pipe` / `Compose` helpers

```csharp
public static class FunctionExtensions
{
    public static TOut Pipe<TIn, TOut>(this TIn value, Func<TIn, TOut> fn);
    public static Task<TOut> PipeAsync<TIn, TOut>(this TIn value, Func<TIn, Task<TOut>> fn);
    public static T Tap<T>(this T value, Action<T> sideEffect);
    public static Func<TIn, TOut> Compose<TIn, TMid, TOut>(this Func<TIn, TMid> first, Func<TMid, TOut> second);
}
```

Enables chaining on plain values (e.g. header-text → parse → page-count) without intermediate locals.

**Namespace decision (Phase 1, empirically verified):** `FunctionExtensions` lives in `AStar.Dev.FunctionalParadigm.Composition`, NOT the flat namespace. A flat-namespace `Tap<T>(this T, Action<T>)` silently wins overload resolution against `ResultExtensions.Tap(onSuccess, onFailure = null)` (C# prefers the candidate with no omitted optional argument) — `result.Tap(x => ...)` binds `x` to the whole `Result<,>` instead of the success value, with no compiler error. Splitting `ResultExtensions.Tap` into fixed-arity overloads instead produces CS0121 at existing call sites. Do not merge the namespaces; the explicit `using ...Composition` is the fix.

**Implicit operators (Phase 1):** `Exceptional<T>` gained `implicit operator` from `T` (→ Success) and `Exception` (→ Failure), mirroring `Result<TResult,TError>`. `Validation<T>` deliberately has none: `T` can itself be a `Func<...>` under `Apply`, making a blanket implicit-from-`T` ambiguous and error-prone.

### 4.5 Domain error DU — lives in the Scrapper project, not FunctionalParadigm

`ScrapeError` is domain-specific; FunctionalParadigm stays generic.

```csharp
// src/AStar.Dev.Wallpaper.Scrapper/Models/ScrapeError.cs
public abstract record ScrapeError(string Message);
public sealed record PageLoadFailed(string Url, string Message) : ScrapeError(Message);
public sealed record PageParseFailed(string HeaderText, string Message) : ScrapeError(Message);
public sealed record ImageDownloadFailed(string ImageUrl, string Message) : ScrapeError(Message);
public sealed record ImageSaveFailed(string Path, string Message) : ScrapeError(Message);
public sealed record ConfigurationSaveFailed(string Message) : ScrapeError(Message);
public sealed record ClassificationFailed(string FileName, string Message) : ScrapeError(Message);
public sealed record UnexpectedError(Exception Exception) : ScrapeError(Exception.Message);
```

Plus `ScrapeErrorFactory` per DU convention, and one logging extension `Result<T, ScrapeError>.LogFailure(ILogger)` so every pipeline logs failures identically.

## 5. Refactor phases

Every phase follows the repo's non-negotiable TDD workflow: **c-sharp-qa writes failing tests → commit → c-sharp-dev implements → all tests green → c-sharp-reviewer reviews → new c-sharp-dev fixes findings**. Tests come first in every phase — there is no after-the-fact test pass. Standing test rules: never against the real database (in-memory SQLite via `IDbContextFactory`), NSubstitute for collaborators, zero-delay strategy injected into workflow tests, Shouldly assertions, `Given…`/`when_…_then_…` naming. One commit (or PR) per phase; `graphify update .` after each phase.

### Phase 0 — Bug fixes (TDD, before any refactor)

Fix B1–B5 from §3.3, each with a failing test first:

- **B1:** test that `RunAsync` starting from a mid-list `SearchString` skips already-visited categories. Fix: pass the filtered list.
- **B2:** test `ScrapedTagRepository.SaveAsync` receives exactly one call. Fix: delete duplicate line.
- **B3:** covered by the Phase 2 merge, but the failing test (first call to `LoadTopWallpapersPageAsync` does not throw) is written now against `TopWallpapersPage`.
- **B4:** delete dead null-branch; behaviour test for sub-directory update.
- **B5:** test that when control-reset fails the workflow is **not** invoked. Fix ahead of the full Phase 6 rework: failure lambda returns a `Fail` result.
- **B6:** DB connection string becomes configurable (`ConnectionStrings:Sqlite` from configuration/user secrets) with `/home/jbarden/Documents/Scrapper/scrapper.db` as the default. Tests: DI-composed connection string uses the configured value when present, the new default otherwise. Fresh database — no data migration; old data re-imported manually via the existing Import buttons after first run.

### Phase 1 — FunctionalParadigm additions

Implement §4.1–4.4 with full test coverage (Match/Map/Bind laws, error accumulation, cancellation passthrough, async overloads). No consumer changes yet — additive, zero risk.

### Phase 2 — Deletions, merges, renames

1. Delete: `SearchWorkflow` (old), `ConfigurationSaverFunctional`, `ImagePageServiceFunctional` (+ its interface), `ImagePageResultFunctional` (+ interface), `TopWallpapersWorkflowFunctional`, `SearchResultsPage` (old twin).
2. Merge `TopWallpapersPageFunctional` into `TopWallpapersPage` (keeping the `page ??=` init fix and the `ITopWallpapersPage` interface, renamed from `ITopWallpapersPageFunctional`).
3. Rename survivors: `SearchWorkflowFunctional` → `SearchWorkflow`, `SearchResultsPageFunctional` → `SearchResultsPage`.
4. Prune the DI registrations in `App.axaml.cs` accordingly.
5. Introduce `ScrapperConstants` (`ImagesPerPage = 24`, `PageNavigationDelay`, `RetryDelay`, thumbnail sizes) and replace magic numbers.

Behaviour-neutral; existing tests must stay green.

### Phase 3 — Extract pure cores + inject functions

The heart of the "make it pure" goal. Each extraction: pure static function (or small static class), exhaustive unit tests, then the I/O shell calls it.

| New pure unit | Extracted from | Signature sketch |
|---|---|---|
| `PageHeaderParser.Parse` | `SearchResultsPage.GetPageInfoAsync` | `string? headerText → Result<PageInfo, ScrapeError>` where `PageInfo(PageCount, ImageCount, SubDirectoryName)` record replaces the tuple |
| `SubscriptionHeaderParser.Parse` | `SubscriptionsImagesListPage.PageInfoAsync` | same shape |
| `TopWallpapersHeaderParser.Parse` | `TopWallpapersPage.PageInfoAsync` | `string? → Result<int, ScrapeError>` |
| `ImageLinkSelector.SelectWanted` | 4 copies of the href-filter loop | `IEnumerable<string?> hrefs → IReadOnlyCollection<string>` (pure; the Playwright `GetAttributeAsync` loop stays in one shared shell method) |
| `TagRules` (new type) | `ImagePage.ProcessTheImageTagsAsync` + its 8 helper predicates | `(IReadOnlyList<TagData> tags, TagRuleContext ctx) → TagOutcome` where `TagOutcome` is a DU: `SkipImage(reasonTags)` \| `Accept(FilePrefix, DirectorySegments, Tags)`. All mutation of `List<string> directoryName` and prefix-string churn becomes fold over immutable state. DB save of scraped tags moves out to the caller. |
| `SearchProgress` state record | `SearchWorkflow` mutable fields | loop state `(SearchConfiguration, ScrapeDirectories, Category)` threaded through pure transition functions `UpdateSearchDetails`, `UpdateTotalPages`, `UpdateSubDirectory`, `FilterSearchCategories` (all already *almost* pure — they become `static` and lose field access) |
| `ScrapedFileNameFactory` | already pure | unchanged, gains `Result` overload only if needed |

Function injection for non-determinism:

- `Func<int, int, int> randomDelayPicker` (or a tiny `IDelayStrategy` with `Task DelayAsync(DelayKind, CancellationToken)`) injected into workflows and `ImagePageService` — removes `Random.Shared`/`new Random()` and `Task.Delay` scatter, makes workflow loops testable without real waits. Default implementation uses `Random.Shared` + `Task.Delay`; tests inject zero-delay.
- `TimeProvider` already injected in `ImagePageService` — extend to `Stopwatch` usage in workflows (`timeProvider.GetTimestamp()`/`GetElapsedTime`).
- `IFileSystem` (Testably, already a dependency of `ImportExportService`) replaces raw `File`/`Directory`/`FileInfo` in `ImageSaveHelper`, `DirectoryHelper`, `ImagePageService` — the helpers become injectable instance classes (`IImageSaver`, keeping `IDirectoryHelper`) instead of statics with hidden I/O.
- `ImageRetrieverHelper` becomes `IImageRetriever` backed by `HttpClient` from DI (`IHttpClientFactory`), returning `Result<byte[], ScrapeError>` instead of the silent-empty-array-on-failure it has today.

### Phase 4 — Convert the live pipeline to Result chains

Target shape (illustrative, not final code):

```csharp
// SearchWorkflow.RunAsync
public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken ct) =>
    searchConfiguration.SearchCategories
        .Pipe(categories => SearchProgressFunctions.FilterSearchCategories(searchConfiguration, categories))
        .Pipe(categories => ProcessCategoriesAsync(categories, ct));

// per category
private Task<Result<Unit, ScrapeError>> ProcessCategoryAsync(SearchProgress progress, CancellationToken ct) =>
    searchResultsPage.LoadSearchPageAsync(progress.SearchString, progress.StartingPage)     // Result<PageHandle, ScrapeError>
        .BindAsync(_ => searchResultsPage.PageInfoAsync())                                   // Result<PageInfo, ScrapeError>
        .MapAsync(info => progress.With(info))                                               // pure
        .BindAsync(p => p.IsUpToDate
            ? SkipAsync(p, ct)
            : VisitAllPagesAsync(p, ct))
        .TapAsync(p => configurationSaver.SaveAsync(p), error => error.Pipe(e => logger.LogFailure(e)));
```

Concrete changes:

1. **Pages** (`SearchResultsPage`, `TopWallpapersPage`, `SubscriptionsImagesListPage`, `ImagePage`): every public method returns `Result<T, ScrapeError>`. Playwright calls wrapped once via `Try.RunAsync(...).ToResult(ex => ScrapeErrorFactory.PageLoad(url, ex))`. The `IResponse?` + `is { Ok: false }` retry dance becomes an explicit `Bind`-with-retry helper (`Result.RetryOnceAsync`). The `page ??=` lazy init collapses into one `EnsurePageAsync()` used by all methods (or better: `IPlaywrightService.GetPageAsync()` returns the ready page and page classes stop caching it).
2. **`ImagePage`** shrinks to: navigate → read tag locators → read attributes → call pure `TagRules` → read image src → return `Result<ScrapedImage, ScrapeError>` where `ScrapedImage(ImageUrl, DirectorySegments, FilePrefix, Tags)` replaces `ImagePageResult` — the skip case becomes a `SkippedImage` DU case or `Option<ScrapedImage>`, ending `Skip`-flag checking. Scraped-tag persistence moves to `ImagePageService` (single save, fixing B2 permanently).
3. **`ImagePageService.ProcessImagePageAsync`** becomes a chain: `GetImageFromPageAsync → Bind(download) → Bind(save) → Tap(broadcast) → Bind(persist FileDetail) → Bind(classify)`, with the per-link retry expressed once around the chain instead of catch-and-recurse. SkiaSharp dimension probing is extracted into a small pure-ish `ImageDimensionReader` (I/O but isolated and injectable).
4. **`ConfigurationSaver.SaveUpdatedConfigurationAsync`** returns `Result<Unit, ScrapeError>` (`ConfigurationSaveFailed`), try/catch deleted; the upsert loop extracts a pure `MergeCategories(existing, updated)` function.
5. **`FileClassificationService.ClassifyAsync`**: `matched` list building is extracted into pure `ClassificationMatcher.Match(pageData, fileDetail, imageTags) → IReadOnlyList<...>`; the stray early `SaveChangesAsync` at line 57 (saves before adding anything) is removed; method returns `Result<Unit, ScrapeError>`.
6. **`ImportExportService`**: already returns `Result<T, string>` — migrate to `Result<T, ScrapeError>` and route DTO checks through `Validation<T>` so *all* problems in an import file are reported at once, not first-failure.

### Phase 5 — Subscriptions + TopWallpapers workflows

Refactor both to the exact shape of the Phase 4 `SearchWorkflow` (they are already structural clones of it):

- Same `Result<Unit, ScrapeError>` signature, same injected delay strategy, same saver.
- Extract the shared page-loop skeleton (`for page in start..total: delay → save progress → load page → get links → process links`) into one generic `PagedScrapeRunner` taking functions: `loadPage(int)`, `getLinks()`, `processLinks(links, ct)` — the three workflows become thin configurations of it. This is the plan's biggest single simplification: three ~60–130-line classes collapse into one runner + three small setups.
- All page delays come from configuration (`ImagePauseInSeconds`) via the injected delay strategy — the hard-coded 2s delay in the search loop is removed (decision, §2).
- UI wiring for these two workflows is **out of scope** — separate piece of work (decision, §2). This plan guarantees they compile, are DI-registered, and are fully tested.

### Phase 6 — MainWindow + composition

1. Rework `OnScrapeSiteFunctionalClicked` chain: failure branch produces `Fail` (B5 fixed properly), UI-state changes isolated in two functions (`DisableControls`, `ResetUI`), workflow selection ready for the two re-wired workflows.
   - **Phase 0 review note (B5):** a standalone repro showed the original chain's `Match` failure lambda already type-inferred to `Result<CancellationToken, string>` via the DU's implicit operator, so the described "success string continues the chain" defect was not reproducible under actual compiler behaviour. The `ScrapeSiteWorkflowDecision` extraction stands regardless — it removes the fragile implicit-conversion-driven type unification and the `cts!.Token` null-forgiving read.
   - **Widen scope to sibling views:** the same `Match(onSuccess: DisableControlsAndClearStatus, onFailure: ex => ex.Message)` pattern exists in `ScrapeConfiguration/ScrapeConfigurationView.axaml.cs`, `Tags/TagsView.axaml.cs`, and `Classifications/ClassificationsView.axaml.cs` — apply the same `ScrapeSiteWorkflowDecision`-style short-circuit + try/finally `ResetUI` treatment there.
2. `App.axaml.cs` DI cleanup: dead registrations removed (done in Phase 2), blocking `Task.Run(...).GetAwaiter().GetResult()` database init replaced with an async-friendly startup, three `Func<TView>` factory registrations collapsed into one generic helper.
3. Validate `ScrapeConfiguration` at startup via `Validation<ScrapeConfiguration>` — invalid config surfaces all errors in the UI instead of failing mid-scrape.
4. Final `c-sharp-reviewer` pass over the whole diff; `graphify update .`.

## 6. Suggested phase → PR mapping

| Phase | Size | Risk | Depends on |
|---|---|---|---|
| 0 Bug fixes | S | Low | — |
| 1 FunctionalParadigm types | M | None (additive) | — |
| 2 Deletions/renames | M | Low | 0 |
| 3 Pure cores + injection | L | Medium | 1, 2 |
| 4 Live pipeline conversion | L | Medium | 3 |
| 5 Workflow unification | M | Medium | 4 |
| 6 MainWindow/composition | M | Low | 4 (5 optional) |

## 7. Open questions

None — all questions resolved; decisions recorded in §2.
