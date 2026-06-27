using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Serilog;
using System.IO.Abstractions;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAnImportExportService
{
    private static readonly string scrapperDirectory     = Path.GetDirectoryName(ApplicationMetadata.FileClassificationsExportFilePath)!;
    private static readonly string scrapperTagsDirectory = Path.GetDirectoryName(ApplicationMetadata.ScrapedTagsExportFilePath)!;

    private const string CelebrityClassificationName = "Test Celebrity";
    private const string NormalClassificationName    = "Test Normal";

    private const string ActionTagValue    = "Action";
    private const string GenreCategory = "Genre";
    private const string ComedyTagValue    = "Comedy";

    private const string ValidClassificationsJson = """
        [
          {
            "createdAt": "2026-06-20T10:11:12",
            "updatedAt": "2026-06-20T13:14:15",
            "id": 1,
            "name": "Test Celebrity",
            "celebrity": true,
            "includeInSearch": true,
            "fileNameParts": [
              {
                "createdAt": "0001-01-01T00:00:00",
                "updatedAt": "0001-01-01T00:00:00",
                "id": 1,
                "text": "Test Celebrity",
                "includeInSearch": true
              }
            ]
          },
          {
            "createdAt": "2026-06-20T10:11:12",
            "updatedAt": "2026-06-20T13:14:15",
            "id": 2,
            "name": "Test Normal",
            "celebrity": false,
            "includeInSearch": true,
            "fileNameParts": [
              {
                "createdAt": "0001-01-01T00:00:00",
                "updatedAt": "0001-01-01T00:00:00",
                "id": 2,
                "text": "Test Normal",
                "includeInSearch": true
              }
            ]
          }
        ]
        """;

    private const string ValidTagsJson = """
        [
          {
            "value": "Action",
            "category": "Genre",
            "includeInSearch": true
          },
          {
            "value": "Comedy",
            "category": "Genre",
            "includeInSearch": false
          }
        ]
        """;

    private readonly MockFileSystem mockFileSystem;
    private readonly ILogger mockLogger;
    private readonly IImportExportService sut;

    public GivenAnImportExportService()
    {
        mockFileSystem = new MockFileSystem();
        mockLogger     = Substitute.For<ILogger>();
        sut            = new ImportExportService(mockFileSystem, mockLogger);
    }

    [Fact]
    public void when_importing_and_file_does_not_exist_then_failure_result_is_returned() =>
        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Fail<List<FileClassificationDomain>, string>>();

    [Fact]
    public void when_importing_and_file_does_not_exist_then_logger_receives_error_call()
    {
        sut.ImportFileClassificationsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_and_file_contains_null_json_then_failure_result_is_returned()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, "null");

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Fail<List<FileClassificationDomain>, string>>();
    }

    [Fact]
    public void when_importing_and_file_contains_null_json_then_logger_receives_error_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, "null");

        sut.ImportFileClassificationsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_valid_classifications_then_result_is_ok()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>();
    }

    [Fact]
    public void when_importing_valid_classifications_then_correct_count_is_returned()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value.Count.ShouldBe(2);
    }

    [Fact]
    public void when_importing_valid_classifications_then_celebrity_classification_name_is_mapped()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value[0].Name.ShouldBe(CelebrityClassificationName);
    }

    [Fact]
    public void when_importing_valid_classifications_then_normal_classification_name_is_mapped()
    {
        SetupValidImportFile();

        sut.ImportFileClassificationsFromFile()
           .ShouldBeOfType<Ok<List<FileClassificationDomain>, string>>()
           .Value[1].Name.ShouldBe(NormalClassificationName);
    }

    [Fact]
    public void when_exporting_classifications_then_file_is_written_to_expected_path()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);

        sut.ExportFileClassificationsToFile(CreateDomainClassifications());

        mockFileSystem.File.Exists(ApplicationMetadata.FileClassificationsExportFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_exporting_classifications_then_logger_receives_information_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);

        sut.ExportFileClassificationsToFile(CreateDomainClassifications());

        mockLogger.Received(1).Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_file_system_throws_during_export_then_exception_is_rethrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, mockLogger);

        var act = () => throwingSut.ExportFileClassificationsToFile([]);

        act.ShouldThrow<IOException>();
    }

    [Fact]
    public void when_file_system_throws_during_export_then_logger_receives_error_call()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, mockLogger);

        Should.Throw<IOException>(() => throwingSut.ExportFileClassificationsToFile([]));

        mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_tags_and_file_does_not_exist_then_failure_result_is_returned() =>
        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Fail<List<ScrapedTagDomain>, string>>();

    [Fact]
    public void when_importing_tags_and_file_does_not_exist_then_logger_receives_error_call()
    {
        sut.ImportScrapedTagsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_tags_and_file_contains_null_json_then_failure_result_is_returned()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, "null");

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Fail<List<ScrapedTagDomain>, string>>();
    }

    [Fact]
    public void when_importing_tags_and_file_contains_null_json_then_logger_receives_error_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, "null");

        sut.ImportScrapedTagsFromFile();

        mockLogger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_importing_valid_tags_then_result_is_ok()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>();
    }

    [Fact]
    public void when_importing_valid_tags_then_correct_count_is_returned()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value.Count.ShouldBe(2);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_value_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].Value.ShouldBe(ActionTagValue);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_category_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].Category.ShouldBe(GenreCategory);
    }

    [Fact]
    public void when_importing_valid_tags_then_first_tag_include_in_search_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[0].IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public void when_importing_valid_tags_then_second_tag_value_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[1].Value.ShouldBe(ComedyTagValue);
    }

    [Fact]
    public void when_importing_valid_tags_then_second_tag_include_in_search_is_mapped()
    {
        SetupValidTagsImportFile();

        sut.ImportScrapedTagsFromFile()
           .ShouldBeOfType<Ok<List<ScrapedTagDomain>, string>>()
           .Value[1].IncludeInSearch.ShouldBeFalse();
    }

    [Fact]
    public void when_exporting_tags_then_file_is_written_to_expected_path()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);

        sut.ExportScrapedTagsToFile(CreateDomainTags());

        mockFileSystem.File.Exists(ApplicationMetadata.ScrapedTagsExportFilePath).ShouldBeTrue();
    }

    [Fact]
    public void when_exporting_tags_then_logger_receives_information_call()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);

        sut.ExportScrapedTagsToFile(CreateDomainTags());

        mockLogger.Received(1).Information(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_file_system_throws_during_tag_export_then_exception_is_rethrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, mockLogger);

        var act = () => throwingSut.ExportScrapedTagsToFile([]);

        act.ShouldThrow<IOException>();
    }

    [Fact]
    public void when_file_system_throws_during_tag_export_then_logger_receives_error_call()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string?>()))
                               .Throw(new IOException("Disk full"));
        var throwingSut = new ImportExportService(throwingFileSystem, mockLogger);

        Should.Throw<IOException>(() => throwingSut.ExportScrapedTagsToFile([]));

        mockLogger.Received(1).Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private void SetupValidImportFile()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, ValidClassificationsJson);
    }

    private void SetupValidTagsImportFile()
    {
        mockFileSystem.Directory.CreateDirectory(scrapperTagsDirectory);
        mockFileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, ValidTagsJson);
    }

    private static List<FileClassificationDomain> CreateDomainClassifications() =>
    [
        new() { Id = 1, Name = CelebrityClassificationName, Celebrity = true,  IncludeInSearch = true },
        new() { Id = 2, Name = NormalClassificationName,    Celebrity = false, IncludeInSearch = true }
    ];

    private static List<ScrapedTagDomain> CreateDomainTags() =>
    [
        new() { Value = ActionTagValue, Category = GenreCategory, IncludeInSearch = true  },
        new() { Value = ComedyTagValue, Category = GenreCategory, IncludeInSearch = false }
    ];
}
