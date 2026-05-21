using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal class ConnectionStringsConfiguration : IComplexPropertyConfiguration<ConnectionStrings>
{
    public void Configure(ComplexPropertyBuilder<ConnectionStrings> builder)
    {
        builder.Property(connectionStrings => connectionStrings.Sqlite)
               .HasColumnType("nvarchar(256)")
               .HasConversion(sqlite => sqlite, sqlite => sqlite);
    }
}