namespace SaaSPlatform.Api.BuildingBlocks.Domain;

public interface IHasTimestamps
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset UpdatedAtUtc { get; set; }
}
