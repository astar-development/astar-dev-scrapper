using AStar.Dev.FunctionalParadigm;
using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using System.IO.Abstractions;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class ImportExportService(IFileSystem fileSystem, ILogger logger) : IImportExportService
{
    public void ExportFileClassificationsToFile(List<FileClassificationDomain> classifications)
    {
        try
        {
            var json = classifications.ToDtos().ToJson();
            fileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, json);
            logger.Information("Classifications exported to {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to export classifications to file: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);
            throw;
        }
    }

    public Result<List<FileClassificationDomain>, string> ImportFileClassificationsFromFile()
    {
        if(!fileSystem.File.Exists(ApplicationMetadata.FileClassificationsExportFilePath))
        {
            logger.Error("File not found: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);

            return $"Error: File not found - {ApplicationMetadata.FileClassificationsExportFilePath}";
        }

        var classificationsJson = fileSystem.File.ReadAllText(ApplicationMetadata.FileClassificationsExportFilePath);
        var classifications = classificationsJson.FromJson<List<FileClassificationDto>>(AStar.Dev.Utilities.Constants.WebDeserialisationSettings);

        if(classifications is null)
        {
            logger.Error("Failed to deserialize classifications from file: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);

            return $"Error: Failed to deserialize classifications from file - {ApplicationMetadata.FileClassificationsExportFilePath}";
        }

        return classifications.ToDomain();
    }
}
