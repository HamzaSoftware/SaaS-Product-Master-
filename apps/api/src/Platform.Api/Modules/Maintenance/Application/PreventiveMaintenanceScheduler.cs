using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Maintenance.Application;

public sealed class PreventiveMaintenanceSchedulerOptions
{
    public const string SectionName = "PreventiveMaintenanceScheduler";
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int GenerateAheadDays { get; set; } = 7;
}

public sealed partial class PreventiveMaintenanceScheduler(IServiceScopeFactory scopeFactory,
    IOptions<PreventiveMaintenanceSchedulerOptions> options, TimeProvider timeProvider,
    ILogger<PreventiveMaintenanceScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled) { SchedulerDisabled(logger); return; }
        var interval = TimeSpan.FromMinutes(Math.Clamp(settings.IntervalMinutes, 1, 1440));
        await RunOnce(settings, stoppingToken);
        using var timer = new PeriodicTimer(interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RunOnce(settings, stoppingToken);
    }

    private async Task RunOnce(PreventiveMaintenanceSchedulerOptions settings, CancellationToken ct)
    {
        try
        {
            List<Guid> tenants;
            using (var discovery = scopeFactory.CreateScope())
            {
                var database = discovery.ServiceProvider.GetRequiredService<PlatformDbContext>();
                tenants = await database.Tenants.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToListAsync(ct);
            }
            var through = timeProvider.GetUtcNow().AddDays(Math.Clamp(settings.GenerateAheadDays, 0, 90)); var generated = 0;
            foreach (var tenantId in tenants)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope(); scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Initialize(tenantId);
                    generated += await scope.ServiceProvider.GetRequiredService<IPreventiveMaintenanceService>().GenerateSystem(through, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { TenantGenerationFailed(logger, ex, tenantId); }
            }
            GenerationCompleted(logger, generated, through);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { SchedulerCycleFailed(logger, ex); }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Preventive maintenance scheduler is disabled.")]
    private static partial void SchedulerDisabled(ILogger logger);
    [LoggerMessage(Level = LogLevel.Error, Message = "Preventive maintenance generation failed for tenant {TenantId}.")]
    private static partial void TenantGenerationFailed(ILogger logger, Exception exception, Guid tenantId);
    [LoggerMessage(Level = LogLevel.Information, Message = "Preventive maintenance generation completed. Generated {Count} work orders through {ThroughUtc}.")]
    private static partial void GenerationCompleted(ILogger logger, int count, DateTimeOffset throughUtc);
    [LoggerMessage(Level = LogLevel.Error, Message = "Preventive maintenance scheduler cycle failed.")]
    private static partial void SchedulerCycleFailed(ILogger logger, Exception exception);
}
