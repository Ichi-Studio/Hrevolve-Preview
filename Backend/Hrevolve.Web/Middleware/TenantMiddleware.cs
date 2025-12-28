namespace Hrevolve.Web.Middleware;

/// <summary>
/// 多租户中间件 - 解析并设置租户上下文
/// </summary>
public class TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
{
    // 不需要租户的路径
    private static readonly string[] ExcludedPaths = 
    [
        "/health",
        "/swagger",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/me",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/api/localization"
    ];
    
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContextAccessor,
        ITenantResolver tenantResolver)
    {
        // 检查是否为排除路径
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (ExcludedPaths.Any(p => path.StartsWith(p)))
        {
            await next(context);
            return;
        }
        
        // 优先从JWT中获取租户ID（已认证用户）
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        logger.LogDebug("JWT tenant_id claim: {TenantId}", tenantIdClaim);
        
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            var tenantInfo = await tenantResolver.GetByIdAsync(tenantId);
            if (tenantInfo != null && tenantInfo.IsActive)
            {
                tenantContextAccessor.TenantContext = new TenantContext(tenantInfo.Id, tenantInfo.Code);
                logger.LogDebug("租户上下文已从JWT设置: {TenantId} ({TenantCode})", tenantInfo.Id, tenantInfo.Code);
                
                try
                {
                    await next(context);
                }
                finally
                {
                    tenantContextAccessor.TenantContext = null;
                }
                return;
            }
        }
        
        // 尝试从请求中解析租户
        var tenantIdentifier = ResolveTenantIdentifier(context);
        logger.LogDebug("从请求解析的租户标识: {Identifier}", tenantIdentifier);
        
        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            throw new TenantException("无法识别租户信息");
        }
        
        // 解析租户
        var tenant = await tenantResolver.ResolveAsync(tenantIdentifier);
        
        if (tenant == null)
        {
            throw new TenantException($"租户 '{tenantIdentifier}' 不存在");
        }
        
        if (!tenant.IsActive)
        {
            throw new TenantException("租户已被禁用");
        }
        
        // 设置租户上下文
        tenantContextAccessor.TenantContext = new TenantContext(tenant.Id, tenant.Code);
        
        logger.LogDebug("租户上下文已设置: {TenantId} ({TenantCode})", tenant.Id, tenant.Code);
        
        try
        {
            await next(context);
        }
        finally
        {
            // 清理租户上下文
            tenantContextAccessor.TenantContext = null;
        }
    }
    
    /// <summary>
    /// 从请求中解析租户标识
    /// 优先级: Header > Subdomain > Query
    /// </summary>
    private static string? ResolveTenantIdentifier(HttpContext context)
    {
        // 1. 从Header获取 (X-Tenant-Id)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
        {
            var value = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        
        // 2. 从子域名获取 (tenant.example.com)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[0]))
        {
            return parts[0];
        }
        
        // 3. 从Query参数获取
        if (context.Request.Query.TryGetValue("tenant", out var queryValue))
        {
            var value = queryValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        
        return null;
    }
}
