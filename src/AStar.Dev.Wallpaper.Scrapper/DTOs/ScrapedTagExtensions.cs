using ScrapedTagDto      = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;
using ScrapedTagDomain   = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;
using ScrapedTagDomainId = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTagId;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public static class ScrapedTagExtensions
{
    public static List<ScrapedTagDomain> ToDomain(this List<ScrapedTagDto> dtos)
        => [.. dtos.Select(dto => new ScrapedTagDomain
        {
            Id = new ScrapedTagDomainId(dto.Id.Value),
            Value = dto.Value,
            Category = dto.Category,
            IncludeInSearch = dto.IncludeInSearch
        })];

    public static List<ScrapedTagDto> ToDtos(this List<ScrapedTagDomain> domains)
        => [.. domains.Select(domain => new ScrapedTagDto
        {
            Id = new ScrapedTagId(domain.Id.Value),
            Value = domain.Value,
            Category = domain.Category,
            IncludeInSearch = domain.IncludeInSearch
        })];
}
