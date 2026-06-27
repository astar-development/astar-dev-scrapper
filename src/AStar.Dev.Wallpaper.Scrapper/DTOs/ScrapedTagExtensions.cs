using ScrapedTagDto      = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;
using ScrapedTagDomain   = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;
using ScrapedTagDomainId = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTagId;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class ScrapedTagExtensions
{
    public static ScrapedTagDomain ToDomain(this ScrapedTagDto dto)
        => new()
        {
            Id = new ScrapedTagDomainId(dto.Id.Value),
            Value = dto.Value,
            Category = dto.Category,
            IncludeInSearch = dto.IncludeInSearch
        };

    public static ScrapedTagDto ToDto(this ScrapedTagDomain domain)
        => new()
        {
            Id = new ScrapedTagId(domain.Id.Value),
            Value = domain.Value,
            Category = domain.Category,
            IncludeInSearch = domain.IncludeInSearch
        };

    public static List<ScrapedTagDomain> ToDomain(this List<ScrapedTagDto> dtos)
        => [.. dtos.Select(dto => dto.ToDomain())];

    public static List<ScrapedTagDto> ToDtos(this List<ScrapedTagDomain> domains)
        => [.. domains.Select(domain => domain.ToDto())];
}
