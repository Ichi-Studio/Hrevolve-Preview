namespace Hrevolve.Web.Middleware;

/// <summary>
/// 当前用户中间件 - 从JWT解析并设置当前用户上下文
/// </summary>
public class CurrentUserMiddleware(RequestDelegate next)
{
    
    public async Task InvokeAsync(HttpContext context, ICurrentUserAccessor currentUserAccessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var currentUser = BuildCurrentUser(context.User);
            currentUserAccessor.CurrentUser = currentUser;
        }
        
        try
        {
            await next(context);
        }
        finally
        {
            currentUserAccessor.CurrentUser = null;
        }
    }
    
    private static CurrentUser BuildCurrentUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? principal.FindFirst("sub")?.Value;
        
        var tenantId = principal.FindFirst("tenant_id")?.Value;
        var employeeId = principal.FindFirst("employee_id")?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst("email")?.Value;
        var userName = principal.FindFirst("username")?.Value;
        
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        
        var permissions = principal.FindAll("permission")
            .Select(c => c.Value)
            .ToList();
        
        return new CurrentUser
        {
            Id = Guid.TryParse(userId, out var uid) ? uid : null,
            TenantId = Guid.TryParse(tenantId, out var tid) ? tid : null,
            EmployeeId = Guid.TryParse(employeeId, out var eid) ? eid : null,
            Email = email,
            UserName = userName,
            Roles = roles,
            Permissions = permissions
        };
    }
}
