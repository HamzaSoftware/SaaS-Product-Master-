using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;

namespace SaaSPlatform.Api.Tests.Tenancy;

public sealed class PlatformDbContextTests
{
    [Fact]
    public async Task SaveChangesAssignsCurrentTenantToNewTenantOwnedEntity()
    {
        var tenantId = Guid.NewGuid();
        await using var database = CreateDatabase(new TestCurrentTenant(tenantId));
        var role = new TenantRole { Id = Guid.NewGuid(), TenantId = Guid.Empty, Name = "Manager" };

        database.TenantRoles.Add(role);
        await database.SaveChangesAsync();

        Assert.Equal(tenantId, role.TenantId);
    }

    [Fact]
    public async Task QueryFilterOnlyReturnsCurrentTenantRecords()
    {
        var firstTenant = Guid.NewGuid();
        var secondTenant = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();

        await using (var firstDatabase = CreateDatabase(new TestCurrentTenant(firstTenant), databaseName))
        {
            firstDatabase.TenantRoles.Add(new TenantRole { Id = Guid.NewGuid(), TenantId = firstTenant, Name = "First" });
            await firstDatabase.SaveChangesAsync();
        }

        await using (var secondDatabase = CreateDatabase(new TestCurrentTenant(secondTenant), databaseName))
        {
            secondDatabase.TenantRoles.Add(new TenantRole { Id = Guid.NewGuid(), TenantId = secondTenant, Name = "Second" });
            await secondDatabase.SaveChangesAsync();
        }

        await using var queryDatabase = CreateDatabase(new TestCurrentTenant(firstTenant), databaseName);
        var visibleRoles = await queryDatabase.TenantRoles.AsNoTracking().ToListAsync();

        Assert.Single(visibleRoles);
        Assert.Equal("First", visibleRoles[0].Name);
    }

    [Fact]
    public async Task SaveChangesRejectsCrossTenantChanges()
    {
        var currentTenant = Guid.NewGuid();
        await using var database = CreateDatabase(new TestCurrentTenant(currentTenant));
        database.TenantRoles.Add(new TenantRole
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Foreign"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => database.SaveChangesAsync());

        Assert.Contains("Cross-tenant", exception.Message, StringComparison.Ordinal);
    }

    private static PlatformDbContext CreateDatabase(ICurrentTenant tenant, string? name = null)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new PlatformDbContext(options, tenant, TimeProvider.System);
    }

    private sealed class TestCurrentTenant(Guid tenantId) : ICurrentTenant
    {
        public Guid? Id { get; private set; } = tenantId;
        public void Initialize(Guid value) => Id = value;
    }
}
