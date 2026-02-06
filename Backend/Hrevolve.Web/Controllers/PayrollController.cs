namespace Hrevolve.Web.Controllers;

/// <summary>
/// 薪酬管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController(Hrevolve.Infrastructure.Persistence.HrevolveDbContext context, ICurrentUserAccessor currentUserAccessor) : ControllerBase
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
        var query = context.PayrollPeriods.AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);

        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new
            {
                id = p.Id,
                name = $"{p.Year}-{p.Month:D2}",
                year = p.Year,
                month = p.Month,
                startDate = p.StartDate,
                endDate = p.EndDate,
                payDate = p.PayDate,
                status = p.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("records")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetPayrollRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? periodId = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery =
            from r in context.PayrollRecords
            join e in context.Employees on r.EmployeeId equals e.Id
            join p in context.PayrollPeriods on r.PayrollPeriodId equals p.Id
            select new { r, e, p };

        if (periodId.HasValue) baseQuery = baseQuery.Where(x => x.p.Id == periodId.Value);
        if (employeeId.HasValue) baseQuery = baseQuery.Where(x => x.e.Id == employeeId.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var raw = await baseQuery
            .OrderByDescending(x => x.p.Year)
            .ThenByDescending(x => x.p.Month)
            .ThenBy(x => x.e.EmployeeNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var recordIds = raw.Select(x => x.r.Id).Distinct().ToList();
        var details = await context.Set<Hrevolve.Domain.Payroll.PayrollDetail>()
            .Where(d => recordIds.Contains(d.PayrollRecordId))
            .ToListAsync(cancellationToken);
        var detailsByRecord = details
            .GroupBy(d => d.PayrollRecordId)
            .ToDictionary(g => g.Key, g => g.Select(d => new
            {
                salaryItemId = d.PayrollItemId,
                salaryItemName = d.ItemName,
                type = d.ItemType.ToString(),
                amount = d.Amount
            }).ToList());

        var employeeIds = raw.Select(x => x.e.Id).Distinct().ToList();
        var deptByEmployee = await (
            from j in context.JobHistories
            where employeeIds.Contains(j.EmployeeId) && j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            join d in context.OrganizationUnits on j.DepartmentId equals d.Id
            select new { j.EmployeeId, d.Name }
        ).ToDictionaryAsync(x => x.EmployeeId, x => x.Name, cancellationToken);

        var items = raw.Select(x => new
        {
            id = x.r.Id,
            employeeId = x.e.Id,
            employeeNo = x.e.EmployeeNumber,
            employeeName = x.e.LastName + x.e.FirstName,
            departmentName = deptByEmployee.TryGetValue(x.e.Id, out var dn) ? dn : null,
            periodId = x.p.Id,
            periodName = $"{x.p.Year}-{x.p.Month:D2}",
            baseSalary = x.r.BaseSalary,
            grossSalary = x.r.GrossSalary,
            netSalary = x.r.NetSalary,
            status = x.r.Status.ToString(),
            items = detailsByRecord.TryGetValue(x.r.Id, out var ds) ? ds : []
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items, total = totalCount, page, pageSize, totalPages });
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
    [HttpPost("calculate")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CalculatePayroll(
        [FromBody] CalculatePayrollRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现薪资计算命令
        return Ok(new { message = request.IsDryRun ? "薪资试算完成" : "薪资计算完成" });
    }
    
    /// <summary>
    /// 按周期执行薪资计算（试算）
    /// </summary>
    [HttpPost("periods/{periodId:guid}/calculate")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CalculatePayrollByPeriod(
        Guid periodId,
        [FromQuery] bool isDryRun = true,
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现薪资计算命令
        return Ok(new { message = isDryRun ? "薪资试算完成" : "薪资计算完成" });
    }
    
    /// <summary>
    /// 审批薪资周期
    /// </summary>
    [HttpPost("periods/{periodId:guid}/approve")]
    [RequirePermission(Permissions.PayrollApprove)]
    public async Task<IActionResult> ApprovePayrollPeriod(
        Guid periodId,
        CancellationToken cancellationToken)
    {
        // TODO: 实现审批薪资周期命令
        return Ok(new { message = "薪资周期已审批" });
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
    /// 获取薪资记录详情
    /// </summary>
    [HttpGet("records/{id:guid}")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetPayrollRecordById(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: 实现获取薪资记录详情查询
        return Ok(new { message = "获取薪资记录详情功能待实现" });
    }
    
    /// <summary>
    /// 获取员工薪资单
    /// </summary>
    [HttpGet("records/employee/{employeeId:guid}")]
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
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetMyPayrollRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue)
        {
            return Ok(new { items = Array.Empty<object>(), total = 0, page, pageSize, totalPages = 0 });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query =
            from r in context.PayrollRecords
            where r.EmployeeId == employeeId.Value
            join e in context.Employees on r.EmployeeId equals e.Id
            join p in context.PayrollPeriods on r.PayrollPeriodId equals p.Id
            select new { r, e, p };

        if (year.HasValue) query = query.Where(x => x.p.Year == year.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(x => x.p.Year)
            .ThenByDescending(x => x.p.Month)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var recordIds = raw.Select(x => x.r.Id).ToList();
        var details = await context.Set<Hrevolve.Domain.Payroll.PayrollDetail>()
            .Where(d => recordIds.Contains(d.PayrollRecordId))
            .ToListAsync(cancellationToken);
        var detailsByRecord = details
            .GroupBy(d => d.PayrollRecordId)
            .ToDictionary(g => g.Key, g => g.Select(d => new
            {
                salaryItemId = d.PayrollItemId,
                salaryItemName = d.ItemName,
                type = d.ItemType.ToString(),
                amount = d.Amount
            }).Cast<object>().ToList());

        var finalItems = raw.Select(x => new
        {
            id = x.r.Id,
            employeeId = x.e.Id,
            employeeNo = x.e.EmployeeNumber,
            employeeName = x.e.LastName + x.e.FirstName,
            departmentName = "",
            periodId = x.p.Id,
            periodName = $"{x.p.Year}-{x.p.Month:D2}",
            baseSalary = x.r.BaseSalary,
            grossSalary = x.r.GrossSalary,
            netSalary = x.r.NetSalary,
            status = x.r.Status.ToString(),
            items = detailsByRecord.TryGetValue(x.r.Id, out var ds) ? ds : []
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items = finalItems, total = totalCount, page, pageSize, totalPages });
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

public record CalculatePayrollRequest(
    Guid PeriodId,
    Guid[]? EmployeeIds,
    bool IsDryRun = true);

public record CreatePayrollItemRequest(
    string Name,
    string Code,
    string Type,
    string CalculationType,
    decimal? FixedAmount,
    string? Formula,
    bool IsTaxable);
