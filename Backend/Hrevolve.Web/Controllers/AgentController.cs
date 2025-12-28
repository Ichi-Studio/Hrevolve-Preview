namespace Hrevolve.Web.Controllers;

/// <summary>
/// AI Agent控制器 - HR智能助手API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController(
    IHrAgentService agentService,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<AgentController> logger) : ControllerBase
{
    
    /// <summary>
    /// 发送消息给HR助手
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = currentUserAccessor.CurrentUser;
        
        if (currentUser?.EmployeeId == null)
        {
            return BadRequest(new { code = "NO_EMPLOYEE", message = "当前用户未关联员工信息" });
        }
        
        var employeeId = currentUser.EmployeeId.Value;
        
        logger.LogInformation("员工 {EmployeeId} 发送消息给HR助手", employeeId);
        
        var response = await agentService.ChatAsync(employeeId, request.Message, cancellationToken);
        
        return Ok(new ChatResponse
        {
            Message = response,
            Timestamp = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// 获取对话历史
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
    {
        var currentUser = currentUserAccessor.CurrentUser;
        
        if (currentUser?.EmployeeId == null)
        {
            return BadRequest(new { code = "NO_EMPLOYEE", message = "当前用户未关联员工信息" });
        }
        
        var history = await agentService.GetChatHistoryAsync(currentUser.EmployeeId.Value, limit);
        
        return Ok(new { messages = history });
    }
    
    /// <summary>
    /// 清除对话历史
    /// </summary>
    [HttpDelete("history")]
    public async Task<IActionResult> ClearHistory()
    {
        var currentUser = currentUserAccessor.CurrentUser;
        
        if (currentUser?.EmployeeId == null)
        {
            return BadRequest(new { code = "NO_EMPLOYEE", message = "当前用户未关联员工信息" });
        }
        
        await agentService.ClearChatHistoryAsync(currentUser.EmployeeId.Value);
        
        return Ok(new { message = "对话历史已清除" });
    }
}

public record ChatRequest(string Message);

public record ChatResponse
{
    public string Message { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}
