namespace Hrevolve.Web.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator, Hrevolve.Infrastructure.Persistence.HrevolveDbContext context) : ControllerBase
{
    
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        // 补充IP地址
        var enrichedCommand = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        
        var result = await mediator.Send(enrichedCommand, cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Error });
        }

        var expiresIn = (int)Math.Max(0, (result.Value.ExpiresAt - DateTime.UtcNow).TotalSeconds);
        return Ok(new
        {
            accessToken = result.Value.AccessToken,
            refreshToken = result.Value.RefreshToken,
            expiresIn,
            userId = result.Value.UserId,
            userName = result.Value.UserName,
            requiresMfa = result.Value.RequiresMfa
        });
    }
    
    /// <summary>
    /// 刷新Token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        // TODO: 实现Token刷新逻辑
        return Ok(new { message = "Token刷新功能待实现" });
    }
    
    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // TODO: 实现登出逻辑（如将Token加入黑名单）
        return Ok(new { message = "登出成功" });
    }
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return Unauthorized();

        var permissions = User.FindAll("permission").Select(c => c.Value).Distinct().ToList();
        var roles = await (
            from ur in context.UserRoles.IgnoreQueryFilters()
            where ur.UserId == userId
            join r in context.Roles.IgnoreQueryFilters() on ur.RoleId equals r.Id
            select r.Name
        ).ToListAsync(cancellationToken);

        if (permissions.Contains(Permissions.SystemAdmin))
        {
            roles = roles.Contains("Admin") ? roles : ["Admin", ..roles];
        }

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            displayName = user.Username,
            roles,
            permissions,
            tenantId = user.TenantId,
            employeeId = user.EmployeeId
        });
    }
}

public record RefreshTokenRequest(string RefreshToken);
