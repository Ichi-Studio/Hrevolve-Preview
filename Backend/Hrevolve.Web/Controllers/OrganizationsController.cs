namespace Hrevolve.Web.Controllers;

/// <summary>
/// 组织架构控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController(Hrevolve.Infrastructure.Persistence.HrevolveDbContext context) : ControllerBase
{
    
    /// <summary>
    /// 获取组织架构树
    /// </summary>
    [HttpGet("tree")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<IActionResult> GetOrganizationTree(CancellationToken cancellationToken)
    {
        var units = await context.OrganizationUnits
            .OrderBy(u => u.Level)
            .ThenBy(u => u.SortOrder)
            .ToListAsync(cancellationToken);

        if (units.Count == 0) return Ok(Array.Empty<object>());

        var currentJobs =
            from j in context.JobHistories
            where j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            join e in context.Employees on j.EmployeeId equals e.Id
            where e.Status != Hrevolve.Domain.Employees.EmploymentStatus.Terminated
            select new { j.DepartmentId };

        var deptCounts = await currentJobs
            .GroupBy(x => x.DepartmentId)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

        var managers = await context.Employees
            .Select(e => new { e.Id, Name = e.LastName + e.FirstName })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var nodes = units.ToDictionary(
            u => u.Id,
            u => new OrgNode
            {
                id = u.Id,
                code = u.Code,
                name = u.Name,
                parentId = u.ParentId,
                path = u.Path,
                level = u.Level,
                sortOrder = u.SortOrder,
                managerId = u.ManagerId,
                managerName = u.ManagerId.HasValue && managers.TryGetValue(u.ManagerId.Value, out var mn) ? mn : null,
                employeeCount = deptCounts.TryGetValue(u.Id, out var c) ? c : 0,
                children = []
            });

        foreach (var node in nodes.Values)
        {
            if (node.parentId.HasValue && nodes.TryGetValue(node.parentId.Value, out var parent))
            {
                parent.children.Add(node);
            }
        }

        var roots = nodes.Values.Where(n => !n.parentId.HasValue).ToList();

        var orderedByLevelDesc = units.OrderByDescending(u => u.Level).Select(u => nodes[u.Id]).ToList();
        foreach (var node in orderedByLevelDesc)
        {
            if (node.parentId.HasValue && nodes.TryGetValue(node.parentId.Value, out var parent))
            {
                parent.employeeCount += node.employeeCount;
            }
        }

        return Ok(roots);
    }
    
    /// <summary>
    /// 获取组织单元详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<IActionResult> GetOrganizationUnit(Guid id, CancellationToken cancellationToken)
    {
        var unit = await context.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (unit == null) return NotFound();
        return Ok(unit);
    }
    
    /// <summary>
    /// 创建组织单元
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.OrganizationWrite)]
    public async Task<IActionResult> CreateOrganizationUnit(
        [FromBody] CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        return BadRequest();
    }
    
    /// <summary>
    /// 更新组织单元
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.OrganizationWrite)]
    public async Task<IActionResult> UpdateOrganizationUnit(
        Guid id,
        [FromBody] UpdateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        return BadRequest();
    }
    
    /// <summary>
    /// 获取组织单元下的员工
    /// </summary>
    [HttpGet("{id:guid}/employees")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<IActionResult> GetOrganizationEmployees(
        Guid id,
        [FromQuery] bool includeSubUnits = false,
        CancellationToken cancellationToken = default)
    {
        return BadRequest();
    }
    
    /// <summary>
    /// 获取所有职位列表
    /// </summary>
    [HttpGet("positions")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<IActionResult> GetAllPositions(CancellationToken cancellationToken)
    {
        var items = await context.Positions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                id = p.Id,
                code = p.Code,
                name = p.Name,
                level = p.Level,
                description = p.Description,
                salaryMin = p.SalaryRangeMin,
                salaryMax = p.SalaryRangeMax,
                isActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        var result = items.Select(p => new
        {
            p.id,
            p.code,
            p.name,
            level = (int)p.level,
            p.description,
            p.salaryMin,
            p.salaryMax,
            p.isActive
        });

        return Ok(result);
    }
    
    /// <summary>
    /// 获取组织单元下的职位
    /// </summary>
    [HttpGet("{id:guid}/positions")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<IActionResult> GetOrganizationPositions(Guid id, CancellationToken cancellationToken)
    {
        var items = await context.Positions
            .AsNoTracking()
            .Where(p => p.OrganizationUnitId == id)
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                id = p.Id,
                code = p.Code,
                name = p.Name,
                level = p.Level,
                description = p.Description,
                salaryMin = p.SalaryRangeMin,
                salaryMax = p.SalaryRangeMax,
                isActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        var result = items.Select(p => new
        {
            p.id,
            p.code,
            p.name,
            level = (int)p.level,
            p.description,
            p.salaryMin,
            p.salaryMax,
            p.isActive
        });

        return Ok(result);
    }

    private sealed class OrgNode
    {
        public Guid id { get; init; }
        public string code { get; init; } = null!;
        public string name { get; init; } = null!;
        public Guid? parentId { get; init; }
        public string path { get; init; } = null!;
        public int level { get; init; }
        public int sortOrder { get; init; }
        public Guid? managerId { get; init; }
        public string? managerName { get; init; }
        public int employeeCount { get; set; }
        public List<OrgNode> children { get; init; } = null!;
    }
}

public record CreateOrganizationUnitRequest(
    string Name,
    string Code,
    string Type,
    Guid? ParentId,
    string? Description);

public record UpdateOrganizationUnitRequest(
    string Name,
    string? Description,
    Guid? ManagerId);
