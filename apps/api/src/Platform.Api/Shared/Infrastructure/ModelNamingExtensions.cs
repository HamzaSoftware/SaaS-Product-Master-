using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure;

public static class ModelNamingExtensions
{
    public static void UseSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName() ?? entity.ClrType.Name;
            if (entity.GetTableName() is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"pk_{tableName}"));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? $"ix_{tableName}"));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        var withWordBoundaries = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1_$2");
        return Regex.Replace(withWordBoundaries, "([A-Z])([A-Z][a-z])", "$1_$2").ToLowerInvariant();
    }
}
