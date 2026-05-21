using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal class ScrapeDirectoriesConfiguration : IComplexPropertyConfiguration<ScrapeDirectories>
{
    public void Configure(ComplexPropertyBuilder<ScrapeDirectories> builder)
    {
        builder.Property(scrapeDirs => scrapeDirs.RootDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirs => scrapeDirs.BaseSaveDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirs => scrapeDirs.BaseDirectory)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirs => scrapeDirs.BaseDirectoryFamous)
               .HasColumnType("nvarchar(256)");

        builder.Property(scrapeDirs => scrapeDirs.SubDirectoryName)
               .HasColumnType("nvarchar(256)");
    }
}
