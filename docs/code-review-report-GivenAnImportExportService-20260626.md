# Code Review Report — `GivenAnImportExportService`

**File reviewed:** `test/AStar.Dev.Wallpaper.Scrapper.Tests.Unit/Services/GivenAnImportExportService.cs`  
**Date:** 2026-06-26  
**Reviewer:** Claude Code (senior C# / .NET engineer)  
**Branch:** `feature/44-db-export-for-classifications`

---

## Issues

---

### 1 · `.csproj` declares `<Nullable>` and NuGet versions inline

**File:** `test/AStar.Dev.Wallpaper.Scrapper.Tests.Unit/AStar.Dev.Wallpaper.Scrapper.Tests.Unit.csproj:6,14-19`  
**Severity:** `error`

`<Nullable>enable</Nullable>` must come from `Directory.Build.props`, not the project file. All five `<PackageReference>` elements carry inline `Version="…"` attributes — versions belong in `Directory.Packages.props` (Central Package Management).

> Reference: [repo conventions — `.csproj` files must NOT declare `<Nullable>`; NuGet versions belong in `Directory.Packages.props`]

**Fix — strip from `.csproj`:**

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <!-- Remove <Nullable> — comes from Directory.Build.props -->
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="NSubstitute" />
  <PackageReference Include="Testably.Abstractions.Testing" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="xunit.runner.visualstudio" />
  <PackageReference Include="Shouldly" />
</ItemGroup>
```

Add versions to `Directory.Packages.props`.

---

### 2 · `IImportExportService` and `ImportExportService` live in the wrong file

**File:** `src/AStar.Dev.Wallpaper.Scrapper/Services/ImportExportService.cs:12-16`  
**Severity:** `error`

Repo convention: one type per file. The interface `IImportExportService` is declared in the same file as the class. It must move to `IImportExportService.cs`.

> Reference: [c-sharp-code-style — "Define one class, record, interface etc. per file, and name the file after the class"]

**Fix:** Create `src/AStar.Dev.Wallpaper.Scrapper/Services/IImportExportService.cs`.

---

### 3 · `ImportExportService` is not `sealed`

**File:** `src/AStar.Dev.Wallpaper.Scrapper/Services/ImportExportService.cs:18`  
**Severity:** `warning`

Concrete service classes should be `sealed` unless inheritance is explicitly designed and justified. Leaving it open invites accidental subclassing and prevents some JIT optimisations.

> Reference: [Microsoft — prefer `sealed` on concrete types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed)

**Fix:**

```csharp
public sealed class ImportExportService(IFileSystem fileSystem, ILogger logger) : IImportExportService
```

---

### 4 · `ExportFileClassificationsToFile` accepts `List<T>` — should be `IReadOnlyList<T>`

**File:** `src/AStar.Dev.Wallpaper.Scrapper/Services/ImportExportService.cs:14,20`  
**Severity:** `warning`

The parameter is only read, never mutated. The interface and implementation should express that contract using an immutable collection type.

> Reference: [c-sharp-code-style — "Use `IReadOnlyList<T>` or `IReadOnlyCollection<T>` when immutability is desired"]

**Fix:**

```csharp
// interface
void ExportFileClassificationsToFile(IReadOnlyList<FileClassificationDomain> classifications);

// implementation
public void ExportFileClassificationsToFile(IReadOnlyList<FileClassificationDomain> classifications)
```

---

### 5 · `ScrapperDirectory` computed with `Path.GetDirectoryName` — use `CombinePaths` utility

**File:** `test/…/GivenAnImportExportService.cs:12`  
**Severity:** `warning`

`Path.GetDirectoryName` can silently return `null` — the `!` suppressor masks that. The repo has `PathOperationExtensions.CombinePath` / `CombinePaths` in `AStar.Dev.Utilities` which throw rather than silently drop path segments.

> Reference: [c-sharp-code-style — "use `CombinePaths` not `Path.Combine`"; Microsoft — `Path.GetDirectoryName` returns `null?`](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getdirectoryname)

**Fix:** Derive the directory directly from the constant in `ApplicationMetadata` rather than calling `GetDirectoryName` at test initialisation time, or expose the directory separately from `ApplicationMetadata`.

---

### 6 · Tests for `ExportFileClassificationsToFile` do not create the directory — test may be order-dependent

**File:** `test/…/GivenAnImportExportService.cs:141-158`  
**Severity:** `warning`

`when_exporting_…` tests call `mockFileSystem.Directory.CreateDirectory(ScrapperDirectory)` before writing, but `when_file_system_throws_during_export_…` tests (lines 161-183) do not — they use a `Substitute.For<IFileSystem>()` which sidesteps this. The real concern is that the two happy-path export tests depend on the directory being created manually. The production `ExportFileClassificationsToFile` never creates the directory; if it is missing at runtime the call will throw. Either:

- Add directory creation to the production method and cover it with a test, **or**  
- Document the pre-condition explicitly.

**Severity escalates if no `Directory.CreateDirectory` guard exists in production code.**

---

### 7 · Duplicate test scaffolding — `throwingFileSystem` / `throwingSut` setup repeated

**File:** `test/…/GivenAnImportExportService.cs:163-170` and `176-179`  
**Severity:** `warning`

`throwingFileSystem` and `throwingSut` are constructed identically in two adjacent test methods. Extract to a helper or a second nested class / shared fixture.

> Reference: [c-sharp-code-style — "Use builders for test setup / test data creation"; DRY]

**Fix — shared private helper:**

```csharp
private ImportExportService CreateThrowingOnWriteSut()
{
    var throwingFileSystem = Substitute.For<IFileSystem>();
    throwingFileSystem.File
        .When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
        .Throw(new IOException("Disk full"));

    return new ImportExportService(throwingFileSystem, mockLogger);
}
```

Then each test becomes a one-liner:

```csharp
[Fact]
public void when_file_system_throws_during_export_then_exception_is_rethrown()
    => Should.Throw<IOException>(() => CreateThrowingOnWriteSut().ExportFileClassificationsToFile([]));

[Fact]
public void when_file_system_throws_during_export_then_logger_receives_error_call()
{
    Should.Throw<IOException>(() => CreateThrowingOnWriteSut().ExportFileClassificationsToFile([]));

    mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
}
```

---

### 8 · `when_file_system_throws_during_export_then_exception_is_rethrown` uses lambda style inconsistently

**File:** `test/…/GivenAnImportExportService.cs:168-170`  
**Severity:** `suggestion`

```csharp
var act = () => throwingSut.ExportFileClassificationsToFile([]);
act.ShouldThrow<IOException>();
```

The rest of the file uses `Should.Throw<T>(() => …)` directly (line 181). Inconsistency reduces readability. Pick one form and apply it everywhere.

---

### 9 · Missing test coverage — `ExportFileClassificationsToFile` happy-path content not verified

**File:** `test/…/GivenAnImportExportService.cs:140-157`  
**Severity:** `warning`

Tests only verify: (a) file exists, (b) logger called. No test asserts the *content* written to the file is valid JSON representing the supplied classifications. A serialisation regression would go undetected.

**Suggested additional test:**

```csharp
[Fact]
public void when_exporting_classifications_then_file_content_is_valid_json()
{
    mockFileSystem.Directory.CreateDirectory(ScrapperDirectory);

    sut.ExportFileClassificationsToFile(CreateDomainClassifications());

    var written = mockFileSystem.File.ReadAllText(ApplicationMetadata.FileClassificationsExportFilePath);
    written.ShouldNotBeNullOrWhiteSpace();
    written.ShouldContain(CelebrityClassificationName);
    written.ShouldContain(NormalClassificationName);
}
```

---

### 10 · `CreateDomainClassifications` returns `List<T>` — should be `IReadOnlyList<T>`

**File:** `test/…/GivenAnImportExportService.cs:192`  
**Severity:** `suggestion`

Helper returns `List<FileClassificationDomain>`. Once issue 4 is fixed (parameter becomes `IReadOnlyList<T>`), this helper signature should match to avoid implicit upcasting noise.

**Fix:**

```csharp
private static IReadOnlyList<FileClassificationDomain> CreateDomainClassifications() => [ … ];
```

---

### 11 · `ApplicationMetadata.FileClassificationsExportFilePath` uses `Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments`

**File:** `src/AStar.Dev.Wallpaper.Scrapper/ApplicationMetadata.cs:3,14`  
**Severity:** `warning`

`Microsoft.VisualBasic` is a legacy namespace with no cross-platform guarantee on non-Windows runtimes. Use `Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)` instead.

> Reference: [Microsoft — `Environment.SpecialFolder`](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder)

**Fix:**

```csharp
public static string FileClassificationsExportFilePath =>
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scrapper", "FileClassifications.json");
```

Remove the `using Microsoft.VisualBasic.FileIO;` import.

---

### 12 · `return` on line 52 not preceded by blank line

**File:** `src/AStar.Dev.Wallpaper.Scrapper/Services/ImportExportService.cs:52`  
**Severity:** `error`

Line 51 closes the `if` block; line 52 is `return classifications.ToDomain();` with no blank line between them.

> Reference: [c-sharp-code-style — "Every `return` statement after a code block must be preceded by a blank line"]

**Fix:**

```csharp
        if(classifications is null)
        {
            logger.Error("Failed to deserialize classifications from file: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);
            return $"Error: Failed to deserialize classifications from file - {ApplicationMetadata.FileClassificationsExportFilePath}";
        }

        return classifications.ToDomain();
```

---

## Summary

| Severity | Count |
|----------|-------|
| `error` | 3 |
| `warning` | 7 |
| `suggestion` | 2 |
| **Total** | **12** |

### Verdict: **Request Changes**

Blockers before merge:

1. `.csproj` must drop `<Nullable>` and inline NuGet versions (issue 1).
2. `IImportExportService` must live in its own file (issue 2).
3. `return` blank-line convention violated in production code (issue 12).

Remaining items are improvements that should be addressed in this PR given they concern code introduced in this branch.

---

### References

- [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Environment.SpecialFolder](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder)
- [Path.GetDirectoryName nullable return](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getdirectoryname)
- [sealed keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/sealed)
- AStar.Dev repo conventions (`CLAUDE.md`, `.claude/rules/c-sharp-code-style.md`)
