using SaaSPlatform.Api.BuildingBlocks.Domain;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.ServiceRequests.Domain;

namespace SaaSPlatform.Api.Modules.Maintenance.Domain;

public enum MaintenancePriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum BreakdownStatus { Submitted = 0, UnderReview = 1, Approved = 2, Rejected = 3, ConvertedToWorkOrder = 4 }
public enum CorrectiveWorkOrderStatus { Open = 0, Assigned = 1, InProgress = 2, PendingVerification = 3, ReturnedForCorrection = 4, Completed = 5, Cancelled = 6 }
public enum RepairOutcome { Resolved = 0, PartiallyResolved = 1, NotResolved = 2 }
public enum MaintenanceReportStatus { Draft = 0, Submitted = 1, Verified = 2, Returned = 3 }
public enum MaintenanceWorkOrderType { Corrective = 0, Preventive = 1 }
public enum PreventivePlanState { Active = 0, Paused = 1, Archived = 2 }
public enum PreventiveRecurrenceType { Daily = 0, Weekly = 1, Monthly = 2, Quarterly = 3, Semiannual = 4, Annual = 5 }
public enum PreventiveOccurrenceStatus { Scheduled = 0, Generated = 1, Skipped = 2 }
public enum ChecklistResult { Pass = 0, Fail = 1, NotApplicable = 2 }

public sealed class BreakdownReport : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Number { get; set; }
    public Guid AssetId { get; set; }
    public Guid SiteId { get; set; }
    public string? LocationSnapshot { get; set; }
    public Guid ReportedById { get; set; }
    public DateTimeOffset ReportedAtUtc { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? ObservedSymptoms { get; set; }
    public DateTimeOffset? DowntimeStartedAtUtc { get; set; }
    public MaintenancePriority Priority { get; set; }
    public BreakdownStatus Status { get; set; } = BreakdownStatus.Submitted;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Asset? Asset { get; set; }
    public PlatformUser? ReportedBy { get; set; }
    public ICollection<BreakdownReview> Reviews { get; set; } = [];
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public ServiceRequest? OriginatingServiceRequest { get; set; }
}

public sealed class BreakdownReview : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BreakdownReportId { get; set; }
    public Guid ReviewerId { get; set; }
    public bool Approved { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ReviewedAtUtc { get; set; }
    public BreakdownReport? BreakdownReport { get; set; }
    public PlatformUser? Reviewer { get; set; }
}

public sealed class CorrectiveWorkOrder : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Number { get; set; }
    public Guid? BreakdownReportId { get; set; }
    public MaintenanceWorkOrderType Type { get; set; } = MaintenanceWorkOrderType.Corrective;
    public Guid? PreventiveMaintenancePlanId { get; set; }
    public Guid? PreventiveMaintenanceOccurrenceId { get; set; }
    public DateTimeOffset? ScheduledDueAtUtc { get; set; }
    public Guid AssetId { get; set; }
    public Guid SiteId { get; set; }
    public string? LocationSnapshot { get; set; }
    public MaintenancePriority Priority { get; set; }
    public required string Description { get; set; }
    public CorrectiveWorkOrderStatus Status { get; set; } = CorrectiveWorkOrderStatus.Open;
    public Guid? AssignedTechnicianId { get; set; }
    public Guid? AssignedById { get; set; }
    public DateTimeOffset? AssignedAtUtc { get; set; }
    public DateTimeOffset? PlannedStartUtc { get; set; }
    public DateTimeOffset? ActualStartUtc { get; set; }
    public DateTimeOffset? ActualCompletionUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public BreakdownReport? BreakdownReport { get; set; }
    public PreventiveMaintenancePlan? PreventiveMaintenancePlan { get; set; }
    public PreventiveMaintenanceOccurrence? PreventiveMaintenanceOccurrence { get; set; }
    public Asset? Asset { get; set; }
    public PlatformUser? AssignedTechnician { get; set; }
    public PlatformUser? AssignedBy { get; set; }
    public MaintenanceReport? MaintenanceReport { get; set; }
    public ICollection<WorkOrderAssignment> Assignments { get; set; } = [];
    public ICollection<WorkOrderActivity> Activities { get; set; } = [];
    public ICollection<PreventiveWorkOrderChecklistResponse> ChecklistResponses { get; set; } = [];
}

public sealed class WorkOrderAssignment : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid AssignedById { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public string? Note { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public PlatformUser? Technician { get; set; }
    public PlatformUser? AssignedBy { get; set; }
}

public sealed class WorkOrderActivity : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string Action { get; set; }
    public string? Details { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public PlatformUser? ActorUser { get; set; }
}

public sealed class MaintenanceReport : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid AssetId { get; set; }
    public Guid TechnicianId { get; set; }
    public string? Diagnosis { get; set; }
    public string? WorkPerformed { get; set; }
    public RepairOutcome? RepairOutcome { get; set; }
    public DateTimeOffset? ActualStartUtc { get; set; }
    public DateTimeOffset? ActualEndUtc { get; set; }
    public int? LaborMinutes { get; set; }
    public string? AdditionalNotes { get; set; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public MaintenanceReportStatus Status { get; set; } = MaintenanceReportStatus.Draft;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public Asset? Asset { get; set; }
    public PlatformUser? Technician { get; set; }
    public ICollection<MaintenanceReportReview> Reviews { get; set; } = [];
}

public sealed class MaintenanceReportReview : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MaintenanceReportId { get; set; }
    public Guid ReviewerId { get; set; }
    public bool Verified { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ReviewedAtUtc { get; set; }
    public MaintenanceReport? MaintenanceReport { get; set; }
    public PlatformUser? Reviewer { get; set; }
}

public sealed class PreventiveMaintenancePlan : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public Guid AssetId { get; set; }
    public Guid SiteId { get; set; }
    public MaintenancePriority Priority { get; set; }
    public DateOnly StartLocalDate { get; set; }
    public TimeOnly ScheduledLocalTime { get; set; }
    public required string TimeZoneId { get; set; }
    public PreventiveRecurrenceType RecurrenceType { get; set; }
    public int RecurrenceInterval { get; set; } = 1;
    public DateTimeOffset NextDueAtUtc { get; set; }
    public DateTimeOffset? LastGeneratedAtUtc { get; set; }
    public PreventivePlanState State { get; set; } = PreventivePlanState.Active;
    public int? EstimatedDurationMinutes { get; set; }
    public Guid? DefaultTechnicianId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Asset? Asset { get; set; }
    public PlatformUser? DefaultTechnician { get; set; }
    public PlatformUser? CreatedBy { get; set; }
    public ICollection<PreventiveMaintenanceChecklistItem> ChecklistItems { get; set; } = [];
    public ICollection<PreventiveMaintenanceOccurrence> Occurrences { get; set; } = [];
}

public sealed class PreventiveMaintenanceChecklistItem : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public int Sequence { get; set; }
    public required string InstructionEn { get; set; }
    public string? InstructionAr { get; set; }
    public bool IsRequired { get; set; }
    public string? Guidance { get; set; }
    public bool IsActive { get; set; } = true;
    public PreventiveMaintenancePlan? Plan { get; set; }
}

public sealed class PreventiveMaintenanceOccurrence : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public DateTimeOffset ScheduledDueAtUtc { get; set; }
    public PreventiveOccurrenceStatus Status { get; set; } = PreventiveOccurrenceStatus.Scheduled;
    public DateTimeOffset? GeneratedAtUtc { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public PreventiveMaintenancePlan? Plan { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
}

public sealed class PreventiveWorkOrderChecklistResponse : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid SourceChecklistItemId { get; set; }
    public int Sequence { get; set; }
    public required string InstructionEn { get; set; }
    public string? InstructionAr { get; set; }
    public bool IsRequired { get; set; }
    public string? Guidance { get; set; }
    public ChecklistResult? Result { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedById { get; set; }
    public DateTimeOffset? RecordedAtUtc { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public PlatformUser? RecordedBy { get; set; }
}
