namespace Hrevolve.Web.Controllers;

/// <summary>
/// 假期管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController(IMediator mediator) : ControllerBase
{
    
    /// <summary>
    /// 提交请假申请
    /// </summary>
    [HttpPost("requests")]
    [RequirePermission(Permissions.LeaveWrite)]
    public async Task<IActionResult> CreateLeaveRequest(
        [FromBody] CreateLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Error });
        }
        
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = result.Value }, new { id = result.Value });
    }
    
    /// <summary>
    /// 获取请假申请详情
    /// </summary>
    [HttpGet("requests/{id:guid}")]
    [RequirePermission(Permissions.LeaveRead)]
    public async Task<IActionResult> GetLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        // TODO: 实现获取请假详情查询
        return Ok(new { message = "获取请假详情功能待实现", id });
    }
    
    /// <summary>
    /// 获取我的请假申请列表
    /// </summary>
    [HttpGet("requests/my")]
    public async Task<IActionResult> GetMyLeaveRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现获取我的请假列表查询
        return Ok(new { message = "获取我的请假列表功能待实现" });
    }
    
    /// <summary>
    /// 审批请假申请
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    [RequirePermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> ApproveLeaveRequest(
        Guid id,
        [FromBody] ApproveLeaveRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现审批请假命令
        return Ok(new { message = "审批请假功能待实现" });
    }
    
    /// <summary>
    /// 拒绝请假申请
    /// </summary>
    [HttpPost("requests/{id:guid}/reject")]
    [RequirePermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> RejectLeaveRequest(
        Guid id,
        [FromBody] RejectLeaveRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现拒绝请假命令
        return Ok(new { message = "拒绝请假功能待实现" });
    }
    
    /// <summary>
    /// 取消请假申请
    /// </summary>
    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelLeaveRequest(
        Guid id,
        [FromBody] CancelLeaveRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现取消请假命令
        return Ok(new { message = "取消请假功能待实现" });
    }
    
    /// <summary>
    /// 获取我的假期余额
    /// </summary>
    [HttpGet("balances/my")]
    public async Task<IActionResult> GetMyLeaveBalances(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // TODO: 实现获取假期余额查询
        return Ok(new { message = "获取假期余额功能待实现" });
    }
    
    /// <summary>
    /// 获取假期类型列表
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken cancellationToken)
    {
        // TODO: 实现获取假期类型查询
        return Ok(new { message = "获取假期类型功能待实现" });
    }
}

public record ApproveLeaveRequest(string? Comments);
public record RejectLeaveRequest(string Reason);
public record CancelLeaveRequest(string Reason);
