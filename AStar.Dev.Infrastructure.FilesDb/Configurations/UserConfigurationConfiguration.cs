using AStar.Dev.Infrastructure.FilesDb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.Infrastructure.FilesDb.Configurations;

internal class UserConfigurationConfiguration : IComplexPropertyConfiguration<UserConfiguration>
{
    public void Configure(ComplexPropertyBuilder<UserConfiguration> builder)
    {
        builder.Property(userConfig => userConfig.Username)
               .HasColumnType("nvarchar(256)")
               .HasConversion(username => username, username => username);
    }
}
