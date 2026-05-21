using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public sealed class ScrapeConfigurationEntityConfiguration : IEntityTypeConfiguration<ScrapeConfigurationEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScrapeConfigurationEntity> builder)
    {
        _ = builder.ToTable("ScrapeConfiguration");

        _ = builder.ComplexProperty(config => config.ConnectionStrings)
                   .Configure(new ConnectionStringsConfiguration());

        _ = builder.ComplexProperty(config => config.UserConfiguration)
                   .Configure(new UserConfigurationConfiguration());

        _ = builder.ComplexProperty(config => config.SearchConfiguration)
                   .Configure(new SearchConfigurationConfiguration());

        _ = builder.ComplexProperty(config => config.ScrapeDirectories)
                   .Configure(new ScrapeDirectoriesConfiguration());
    }
}
