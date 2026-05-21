using System.Text.Json;
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

        builder.Property(searchConfig => searchConfig.SearchCategories)
                .HasColumnType("nvarchar(256)")
                .HasConversion(
                     searchCategories => JsonSerializer.Serialize(searchCategories, (JsonSerializerOptions?)null),
                     searchCategoriesJson => (JsonSerializer.Deserialize<List<SearchCategories>>(searchCategoriesJson, (JsonSerializerOptions?)null) ?? new List<SearchCategories>()).ToArray());

        builder.Property(searchConfig => searchConfig.ImagePauseInSeconds)    
               .HasColumnType("int")
               .HasConversion(requestDelay => requestDelay, requestDelay => requestDelay);

               builder.Property(searchConfig => searchConfig.StartingPageNumber)
                      .HasColumnType("int");

                      builder.Property(searchConfig => searchConfig.TotalPages)
                      .HasColumnType("int");

                      builder.Property(searchConfig => searchConfig.SubscriptionsStartingPageNumber)
                      .HasColumnType("int");

                      builder.Property(searchConfig => searchConfig.SubscriptionsTotalPages)
                      .HasColumnType("int");

                      builder.Property(searchConfig => searchConfig.TopWallpapersStartingPageNumber)
                      .HasColumnType("int");

                        builder.Property(searchConfig => searchConfig.TopWallpapersTotalPages)
                        .HasColumnType("int");

                        builder.Property(searchConfig => searchConfig.TopWallpapersStartingPageNumber)
                        .HasColumnType("int");

               builder.Property(searchConfig => searchConfig.BaseUrl).HasColumnType("nvarchar(256)").HasConversion(baseUrl => baseUrl, baseUrl => baseUrl);

        builder.Property(searchConfig => searchConfig.SearchCategories)
               .HasColumnType("nvarchar(256)")
               .HasConversion(
                    searchCategories => JsonSerializer.Serialize(searchCategories, (JsonSerializerOptions?)null),
                    searchCategoriesJson => (JsonSerializer.Deserialize<List<SearchCategories>>(searchCategoriesJson, (JsonSerializerOptions?)null) ?? new List<SearchCategories>()).ToArray());

        builder.Property(searchConfig => searchConfig.ApiKey)
               .HasColumnType("nvarchar(256)")
               .HasConversion(apiKey => apiKey, apiKey => apiKey);

        builder.Property(searchConfig => searchConfig.SearchString)
               .HasColumnType("nvarchar(256)")                                                                                          
               .HasConversion(searchString => searchString, searchString => searchString);

        builder.Property(searchConfig => searchConfig.TopWallpapers)
               .HasColumnType("nvarchar(256)")
               .HasConversion(topWallpapers => topWallpapers, topWallpapers => topWallpapers);

        builder.Property(searchConfig => searchConfig.SearchStringPrefix)
               .HasColumnType("nvarchar(256)")
               .HasConversion(searchStringPrefix => searchStringPrefix, searchStringPrefix => searchStringPrefix);

        builder.Property(searchConfig => searchConfig.SearchStringSuffix)
               .HasColumnType("nvarchar(256)")
               .HasConversion(searchStringSuffix => searchStringSuffix, searchStringSuffix => searchStringSuffix);

               builder.Property(searchConfig => searchConfig.Subscriptions)
                .HasColumnType("nvarchar(256)");

    }
}
