using StoreManagementAPI.Services;

namespace StoreManagementAPI.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            // Try to get CompanyId from a request header (e.g., X-Company-ID)
            if (context.Request.Headers.TryGetValue("X-Company-ID", out var companyIdHeader))
            {
                if (Guid.TryParse(companyIdHeader.ToString(), out Guid companyId))
                {
                    tenantContext.CompanyId = companyId;
                }
            }

            await _next(context);
        }
    }
}
