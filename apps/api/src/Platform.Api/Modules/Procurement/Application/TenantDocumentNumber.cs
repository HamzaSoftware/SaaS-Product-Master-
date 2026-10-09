using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Procurement.Domain;

namespace SaaSPlatform.Api.Modules.Procurement.Application;

public static class TenantDocumentNumber
{
    public static async Task<string> Next(PlatformDbContext database, Guid tenantId, DateTimeOffset now, string type, CancellationToken ct)
    {
        var year = now.Year;
        var sequence = await database.ProcurementDocumentSequences.SingleOrDefaultAsync(x => x.DocumentType == type && x.Year == year, ct);
        if (sequence is null)
        {
            sequence = new ProcurementDocumentSequence { Id = Guid.NewGuid(), TenantId = tenantId, DocumentType = type, Year = year };
            database.ProcurementDocumentSequences.Add(sequence);
        }
        var value = sequence.NextValue++;
        sequence.Version = Guid.NewGuid();
        return $"{type}-{year}-{value:000000}";
    }
}
