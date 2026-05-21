using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal class SearchConfigurationConfiguration : IComplexPropertyConfiguration<SearchConfiguration>
{
    public void Configure(ComplexPropertyBuilder<SearchConfiguration> builder)
    {
        builder.Property(searchConfig => searchConfig.BaseUrl)
               .HasColumnType("nvarchar(256)")
               .HasConversion(baseUrl => baseUrl, baseUrl => baseUrl);
    }
}
