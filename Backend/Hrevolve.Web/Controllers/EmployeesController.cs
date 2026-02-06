namespace Hrevolve.Web.Controllers;

/// <summary>
/// 员工管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(Hrevolve.Infrastructure.Persistence.HrevolveDbContext context) : ControllerBase
{
    
    /// <summary>
    /// 获取员工列表
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int total = 0,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery =
            from e in context.Employees
            join j in context.JobHistories on e.Id equals j.EmployeeId
            where j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            join dept in context.OrganizationUnits on j.DepartmentId equals dept.Id
            join pos in context.Positions on j.PositionId equals pos.Id
            select new
            {
                e.Id,
                e.EmployeeNumber,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Phone,
                Status = e.Status.ToString(),
                e.HireDate,
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                PositionId = pos.Id,
                PositionName = pos.Name
            };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            baseQuery = baseQuery.Where(x =>
                x.EmployeeNumber.Contains(k) ||
                (x.LastName + x.FirstName).Contains(k) ||
                (x.Email != null && x.Email.Contains(k)) ||
                (x.Phone != null && x.Phone.Contains(k)));
        }

        if (departmentId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery = baseQuery.Where(x => x.Status == status);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderBy(x => x.EmployeeNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                id = x.Id,
                employeeNo = x.EmployeeNumber,
                firstName = x.FirstName,
                lastName = x.LastName,
                fullName = x.LastName + x.FirstName,
                email = x.Email ?? string.Empty,
                phone = x.Phone,
                departmentId = x.DepartmentId,
                departmentName = x.DepartmentName,
                positionId = x.PositionId,
                positionName = x.PositionName,
                status = x.Status,
                hireDate = x.HireDate
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new
        {
            items,
            total = totalCount,
            page,
            pageSize,
            totalPages
        });
    }
    
    /// <summary>
    /// 获取员工详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dto = await BuildEmployeeDtoAsync(id, today, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
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
        var dto = await BuildEmployeeDtoAsync(id, date, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("{id:guid}/job-history")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetJobHistory(Guid id, CancellationToken cancellationToken)
    {
        var items =
            await (from j in context.JobHistories
                where j.EmployeeId == id && j.CorrectionStatus != Hrevolve.Domain.Employees.CorrectionStatus.Voided
                join dept in context.OrganizationUnits on j.DepartmentId equals dept.Id
                join pos in context.Positions on j.PositionId equals pos.Id
                orderby j.EffectiveStartDate descending
                select new
                {
                    id = j.Id,
                    employeeId = j.EmployeeId,
                    positionId = j.PositionId,
                    positionName = pos.Name,
                    departmentId = j.DepartmentId,
                    departmentName = dept.Name,
                    salary = j.BaseSalary,
                    effectiveStartDate = j.EffectiveStartDate,
                    effectiveEndDate = j.EffectiveEndDate,
                    changeReason = j.ChangeReason
                }).ToListAsync(cancellationToken);

        return Ok(items);
    }

    private async Task<object?> BuildEmployeeDtoAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null) return null;

        var job = await context.JobHistories
            .Where(j => j.EmployeeId == employeeId && j.CorrectionStatus != Hrevolve.Domain.Employees.CorrectionStatus.Voided)
            .Where(j => j.EffectiveStartDate <= date && j.EffectiveEndDate >= date)
            .OrderByDescending(j => j.EffectiveStartDate)
            .FirstOrDefaultAsync(cancellationToken);

        Guid positionId = Guid.Empty;
        string? positionName = null;
        Guid departmentId = Guid.Empty;
        string? departmentName = null;

        if (job != null)
        {
            positionId = job.PositionId;
            departmentId = job.DepartmentId;

            positionName = await context.Positions.Where(p => p.Id == positionId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);
            departmentName = await context.OrganizationUnits.Where(d => d.Id == departmentId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken);
        }

        string? managerName = null;
        if (employee.DirectManagerId.HasValue)
        {
            managerName = await context.Employees
                .Where(m => m.Id == employee.DirectManagerId.Value)
                .Select(m => m.LastName + m.FirstName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new
        {
            id = employee.Id,
            employeeNo = employee.EmployeeNumber,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            fullName = employee.FullName,
            email = employee.Email ?? string.Empty,
            phone = employee.Phone,
            gender = employee.Gender.ToString(),
            birthDate = employee.DateOfBirth,
            hireDate = employee.HireDate,
            terminationDate = employee.TerminationDate,
            status = employee.Status.ToString(),
            employmentType = employee.EmploymentType.ToString(),
            departmentId,
            departmentName,
            positionId,
            positionName,
            managerId = employee.DirectManagerId,
            managerName
        };
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
        return BadRequest();
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
        return BadRequest();
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
        return BadRequest();
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
