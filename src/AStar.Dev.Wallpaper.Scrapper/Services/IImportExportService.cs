using AStar.Dev.FunctionalParadigm;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IImportExportService
{
    void ExportFileClassificationsToFile(List<FileClassificationDomain> classifications);
    Result<List<FileClassificationDomain>, string> ImportFileClassificationsFromFile();
}
