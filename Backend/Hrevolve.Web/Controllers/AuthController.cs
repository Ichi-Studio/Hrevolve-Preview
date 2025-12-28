namespace Hrevolve.Web.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
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
        
        return Ok(result.Value);
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
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var username = User.FindFirst("username")?.Value;
        var email = User.FindFirst("email")?.Value;
        var employeeId = User.FindFirst("employee_id")?.Value;
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();
        
        return Ok(new
        {
            userId,
            tenantId,
            username,
            email,
            employeeId,
            permissions
        });
    }
}

public record RefreshTokenRequest(string RefreshToken);
