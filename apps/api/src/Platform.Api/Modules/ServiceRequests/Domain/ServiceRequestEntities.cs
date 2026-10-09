using SaaSPlatform.Api.BuildingBlocks.Domain;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;

namespace SaaSPlatform.Api.Modules.ServiceRequests.Domain;

public enum ServiceRequestPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }
public enum ServiceRequestStatus { Draft = 0, Submitted = 1, UnderReview = 2, Assigned = 3, InProgress = 4, Resolved = 5, Closed = 6, Rejected = 7, Cancelled = 8, ConvertedToMaintenance = 9 }
public enum ServiceRequestNoteVisibility { Internal = 0, RequesterVisible = 1 }

public sealed class ServiceRequestCategory : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public ServiceRequestPriority DefaultPriority { get; set; } = ServiceRequestPriority.Normal;
    public bool AssetRequired { get; set; }
    public bool LocationRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ServiceRequest : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Number { get; set; }
    public Guid SiteId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid RequesterId { get; set; }
    public string? RequesterContact { get; set; }
    public Guid? AssetId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ServiceRequestPriority Priority { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Draft;
    public Guid? AssignedToId { get; set; }
    public Guid? AssignedById { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? FirstResponseAtUtc { get; set; }
    public DateTimeOffset? AssignedAtUtc { get; set; }
    public DateTimeOffset? WorkStartedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ResolvedById { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? TerminalReason { get; set; }
    public Guid? LinkedBreakdownReportId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Site? Site { get; set; }
    public LocationNode? Location { get; set; }
    public ServiceRequestCategory? Category { get; set; }
    public PlatformUser? Requester { get; set; }
    public Asset? Asset { get; set; }
    public PlatformUser? AssignedTo { get; set; }
    public PlatformUser? AssignedBy { get; set; }
    public PlatformUser? ResolvedBy { get; set; }
    public BreakdownReport? LinkedBreakdownReport { get; set; }
    public ICollection<ServiceRequestAssignment> Assignments { get; set; } = [];
    public ICollection<ServiceRequestNote> Notes { get; set; } = [];
    public ICollection<ServiceRequestActivity> Activities { get; set; } = [];
}

public sealed class ServiceRequestAssignment : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid? PreviousAssigneeId { get; set; }
    public Guid AssigneeId { get; set; }
    public Guid AssignedById { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public string? Note { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public PlatformUser? PreviousAssignee { get; set; }
    public PlatformUser? Assignee { get; set; }
    public PlatformUser? AssignedBy { get; set; }
}

public sealed class ServiceRequestNote : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Content { get; set; }
    public ServiceRequestNoteVisibility Visibility { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public PlatformUser? Author { get; set; }
}

public sealed class ServiceRequestActivity : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceRequestId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string Action { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }
    public PlatformUser? ActorUser { get; set; }
}
