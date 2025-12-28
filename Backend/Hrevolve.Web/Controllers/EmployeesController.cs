namespace Hrevolve.Web.Controllers;

/// <summary>
/// 员工管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(IMediator mediator) : ControllerBase
{
    
    /// <summary>
    /// 获取员工详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmployeeQuery(id), cancellationToken);
        
        if (result.IsFailure)
        {
            return NotFound(new { code = result.ErrorCode, message = result.Error });
        }
        
        return Ok(result.Value);
    }
    
    /// <summary>
    /// 获取员工在指定日期的状态（历史时点查询）
    /// </summary>
    [HttpGet("{id:guid}/at-date")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetEmployeeAtDate(
        Guid id, 
        [FromQuery] DateOnly date, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmployeeAtDateQuery(id, date), cancellationToken);
        
        if (result.IsFailure)
        {
            return NotFound(new { code = result.ErrorCode, message = result.Error });
        }
        
        return Ok(result.Value);
    }
    
    /// <summary>
    /// 创建员工
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Error });
        }
        
        return CreatedAtAction(nameof(GetEmployee), new { id = result.Value }, new { id = result.Value });
    }
    
    /// <summary>
    /// 更新员工信息
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> UpdateEmployee(
        Guid id, 
        [FromBody] UpdateEmployeeRequest request, 
        CancellationToken cancellationToken)
    {
        // TODO: 实现更新员工命令
        return Ok(new { message = "员工更新功能待实现" });
    }
    
    /// <summary>
    /// 员工离职
    /// </summary>
    [HttpPost("{id:guid}/terminate")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> TerminateEmployee(
        Guid id, 
        [FromBody] TerminateEmployeeRequest request, 
        CancellationToken cancellationToken)
    {
        // TODO: 实现员工离职命令
        return Ok(new { message = "员工离职功能待实现" });
    }
}

public record UpdateEmployeeRequest(
    string? Email,
    string? Phone,
    string? Address,
    Guid? DirectManagerId);

public record TerminateEmployeeRequest(
    DateOnly TerminationDate,
    string Reason);
