using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Tenancy.Infrastructure;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
    {
        var claimTenantId = context.User.GetTenantId();
        var header = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (claimTenantId is not null)
        {
            if (Guid.TryParse(header, out var headerTenantId) && headerTenantId != claimTenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Tenant context does not match the authenticated session.",
                    Type = "https://httpstatuses.com/403"
                });
                return;
            }

            currentTenant.Initialize(claimTenantId.Value);
        }

        await next(context);
    }
}
