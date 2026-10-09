namespace SaaSPlatform.Api.BuildingBlocks.Modules;

public static class ModuleCatalog
{
    public static readonly IReadOnlyCollection<string> Names =
    [
        "Identity", "Tenancy", "Organization", "MasterData", "Locations", "Assets",
        "Maintenance", "PreventiveMaintenance", "Inventory", "Notifications", "Analytics",
        "DataExchange", "Integrations"
    ];
}
