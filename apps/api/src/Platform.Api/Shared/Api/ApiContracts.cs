namespace SaaSPlatform.Api.BuildingBlocks.Api;

public sealed record PageRequest(int Page = 1, int PageSize = 25, string? Search = null, string? Sort = null)
{
    public int NormalizedPage => Math.Max(Page, 1);
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 200);
}

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long TotalCount);
