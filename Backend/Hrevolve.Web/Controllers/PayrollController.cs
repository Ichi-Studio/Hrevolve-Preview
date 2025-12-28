namespace Hrevolve.Web.Controllers;

/// <summary>
/// 薪酬管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController(IMediator mediator) : ControllerBase
{
    
    /// <summary>
    /// 获取薪资周期列表
    /// </summary>
    [HttpGet("periods")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetPayrollPeriods(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // TODO: 实现获取薪资周期列表查询
        return Ok(new { message = "获取薪资周期列表功能待实现" });
    }
    
    /// <summary>
    /// 创建薪资周期
    /// </summary>
    [HttpPost("periods")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CreatePayrollPeriod(
        [FromBody] CreatePayrollPeriodRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现创建薪资周期命令
        return Ok(new { message = "创建薪资周期功能待实现" });
    }
    
    /// <summary>
    /// 执行薪资计算（试算）
    /// </summary>
    [HttpPost("periods/{periodId:guid}/calculate")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CalculatePayroll(
        Guid periodId,
        [FromQuery] bool isDryRun = true,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现薪资计算命令
        return Ok(new { message = isDryRun ? "薪资试算完成" : "薪资计算完成" });
    }
    
    /// <summary>
    /// 锁定薪资周期
    /// </summary>
    [HttpPost("periods/{periodId:guid}/lock")]
    [RequirePermission(Permissions.PayrollApprove)]
    public async Task<IActionResult> LockPayrollPeriod(
        Guid periodId,
        CancellationToken cancellationToken)
    {
        // TODO: 实现锁定薪资周期命令
        return Ok(new { message = "薪资周期已锁定" });
    }
    
    /// <summary>
    /// 获取员工薪资单
    /// </summary>
    [HttpGet("records/{employeeId:guid}")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetEmployeePayrollRecords(
        Guid employeeId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // TODO: 实现获取员工薪资单查询
        return Ok(new { message = "获取员工薪资单功能待实现" });
    }
    
    /// <summary>
    /// 获取我的薪资单
    /// </summary>
    [HttpGet("records/my")]
    public async Task<IActionResult> GetMyPayrollRecords(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        // TODO: 实现获取我的薪资单查询
        return Ok(new { message = "获取我的薪资单功能待实现" });
    }
    
    /// <summary>
    /// 获取薪资项配置
    /// </summary>
    [HttpGet("items")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetPayrollItems(CancellationToken cancellationToken)
    {
        // TODO: 实现获取薪资项配置查询
        return Ok(new { message = "获取薪资项配置功能待实现" });
    }
    
    /// <summary>
    /// 配置薪资项
    /// </summary>
    [HttpPost("items")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CreatePayrollItem(
        [FromBody] CreatePayrollItemRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现创建薪资项命令
        return Ok(new { message = "创建薪资项功能待实现" });
    }
}

public record CreatePayrollPeriodRequest(
    int Year,
    int Month,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly PayDate);

public record CreatePayrollItemRequest(
    string Name,
    string Code,
    string Type,
    string CalculationType,
    decimal? FixedAmount,
    string? Formula,
    bool IsTaxable);
