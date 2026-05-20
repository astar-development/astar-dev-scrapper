using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

/// <summary>
/// </summary>
public class TagToIgnoreConfiguration : IEntityTypeConfiguration<TagToIgnore>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TagToIgnore> builder)
    {
        _ = builder
           .ToTable(nameof(TagToIgnore), Constants.SchemaName)
           .HasKey(fileDetail => fileDetail.Id);

        builder.Property(fileDetail => fileDetail.Value).HasMaxLength(300);
    }
}
