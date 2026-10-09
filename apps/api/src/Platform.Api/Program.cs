using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Identity.Endpoints;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Infrastructure;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Application;
using SaaSPlatform.Api.Modules.Assets.Endpoints;
using SaaSPlatform.Api.Modules.Locations.Application;
using SaaSPlatform.Api.Modules.Locations.Endpoints;
using SaaSPlatform.Api.Modules.Tenancy.Endpoints;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Endpoints;
using SaaSPlatform.Api.Modules.Inventory.Application;
using SaaSPlatform.Api.Modules.Inventory.Endpoints;
using SaaSPlatform.Api.Modules.Procurement.Application;
using SaaSPlatform.Api.Modules.Procurement.Endpoints;
using SaaSPlatform.Api.Modules.ServiceRequests.Application;
using SaaSPlatform.Api.Modules.ServiceRequests.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("PlatformDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:PlatformDatabase is required.");
builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentityCore<PlatformUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<PlatformDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOptions<AuthenticationOptions>()
    .BindConfiguration(AuthenticationOptions.SectionName)
    .Validate(x => Encoding.UTF8.GetByteCount(x.SigningKey) >= 32, "Authentication signing key must be at least 32 bytes.")
    .Validate(
        x => !builder.Environment.IsProduction() || !x.SigningKey.StartsWith("replace-", StringComparison.OrdinalIgnoreCase),
        "Production requires a secret Authentication:SigningKey supplied through configuration or an environment variable.")
    .ValidateOnStart();
var authSettings = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = authSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                if (context.Principal is null || !await validator.ValidatePrincipal(context.Principal, context.HttpContext.RequestAborted))
                    context.Fail("The session or authorization state is no longer valid.");
            }
        };
    });
var authorization = builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PlatformAccess", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(TenantClaims.TenantId)
        .RequireClaim(TenantClaims.TenantId))
    .AddPolicy("PlatformAdministrator", policy => policy.RequireAuthenticatedUser()
        .RequireClaim(TenantClaims.PlatformAdmin, bool.TrueString));
foreach (var permission in TenantPermissions.All)
{
    authorization.AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
        .RequireClaim(TenantClaims.TenantId).RequireClaim(TenantClaims.Permission, permission));
}
foreach (var permission in PlatformPermissions.All)
{
    authorization.AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
        .RequireClaim(TenantClaims.PlatformAdmin, bool.TrueString).RequireClaim(TenantClaims.Permission, permission));
}
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IIdentityAdministrationService, IdentityAdministrationService>();
builder.Services.AddScoped<ICurrentAccessService, CurrentAccessService>();
builder.Services.AddScoped<ISiteScopeAuthorizer, SiteScopeAuthorizer>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<ITenantAdministrationService, TenantAdministrationService>();
builder.Services.AddScoped<IPlatformTenantService, PlatformTenantService>();
builder.Services.AddScoped<IAssetRegistryService, AssetRegistryService>();
builder.Services.AddScoped<ICorrectiveMaintenanceService, CorrectiveMaintenanceService>();
builder.Services.AddScoped<IPreventiveMaintenanceService, PreventiveMaintenanceService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddOptions<PreventiveMaintenanceSchedulerOptions>().BindConfiguration(PreventiveMaintenanceSchedulerOptions.SectionName)
    .Validate(x => x.IntervalMinutes is >= 1 and <= 1440, "Scheduler interval must be between 1 and 1440 minutes.")
    .Validate(x => x.GenerateAheadDays is >= 0 and <= 90, "Generate-ahead days must be between 0 and 90.")
    .ValidateOnStart();
builder.Services.AddHostedService<PreventiveMaintenanceScheduler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(options => options.AddPolicy("auth", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        })));
builder.Services.AddHealthChecks().AddDbContextCheck<PlatformDbContext>("postgresql", tags: ["ready"]);

var app = builder.Build();

if (args.Contains("--bootstrap-platform-admin", StringComparer.Ordinal))
{
    await PlatformAdminBootstrap.Run(app.Services, app.Configuration, CancellationToken.None);
    return;
}

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("WebClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/api/v1", () => TypedResults.Ok(new { Name = "SaaS Platform API", Version = "v1" }))
    .AllowAnonymous()
    .WithTags("Platform");
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapAuthenticationEndpoints();
app.MapContextEndpoints();
app.MapIdentityAdministrationEndpoints();
app.MapPlatformTenantEndpoints();
app.MapTenantAdministrationEndpoints();
app.MapAssetEndpoints();
app.MapCorrectiveMaintenanceEndpoints();
app.MapPreventiveMaintenanceEndpoints();
app.MapInventoryEndpoints();
app.MapProcurementEndpoints();
app.MapServiceRequestEndpoints();

app.Run();

public partial class Program;
