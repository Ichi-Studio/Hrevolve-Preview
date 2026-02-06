namespace Hrevolve.Web.Controllers;

/// <summary>
/// 报销管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController(Hrevolve.Infrastructure.Persistence.HrevolveDbContext context, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    private static readonly object ExpenseTypesLock = new();
    private static List<ExpenseTypeDto> ExpenseTypesCache = DemoTypes();

    /// <summary>
    /// 获取报销类型列表
    /// </summary>
    [HttpGet("types")]
    [RequirePermission("expense:read")]
    public IActionResult GetExpenseTypes([FromQuery] string? category = null, [FromQuery] bool? isActive = null)
    {
        lock (ExpenseTypesLock)
        {
            var query = ExpenseTypesCache.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => string.Equals(t.category, category, StringComparison.OrdinalIgnoreCase));
            }
            if (isActive.HasValue)
            {
                query = query.Where(t => t.isActive == isActive.Value);
            }
            return Ok(query.ToList());
        }
    }

    [HttpGet("types/{id:guid}")]
    [RequirePermission("expense:read")]
    public IActionResult GetExpenseType(Guid id)
    {
        lock (ExpenseTypesLock)
        {
            var item = ExpenseTypesCache.FirstOrDefault(t => t.id == id);
            return item == null ? NotFound() : Ok(item);
        }
    }

    /// <summary>
    /// 创建报销类型
    /// </summary>
    [HttpPost("types")]
    [RequirePermission("expense:write")]
    public IActionResult CreateExpenseType([FromBody] ExpenseTypeUpsert data)
    {
        var created = new ExpenseTypeDto(
            Guid.NewGuid(),
            data.code ?? $"CUSTOM-{Guid.NewGuid():N}".Substring(0, 12),
            data.name ?? "自定义类型",
            data.category ?? "Other",
            data.maxAmount,
            data.requiresReceipt ?? false,
            data.isActive ?? true,
            data.description);

        lock (ExpenseTypesLock)
        {
            ExpenseTypesCache = [created, ..ExpenseTypesCache];
        }

        return Ok(created);
    }

    /// <summary>
    /// 更新报销类型
    /// </summary>
    [HttpPut("types/{id:guid}")]
    [RequirePermission("expense:write")]
    public IActionResult UpdateExpenseType(Guid id, [FromBody] ExpenseTypeUpsert data)
    {
        lock (ExpenseTypesLock)
        {
            var idx = ExpenseTypesCache.FindIndex(t => t.id == id);
            if (idx < 0) return NotFound();

            var old = ExpenseTypesCache[idx];
            var updated = old with
            {
                code = data.code ?? old.code,
                name = data.name ?? old.name,
                category = data.category ?? old.category,
                maxAmount = data.maxAmount ?? old.maxAmount,
                requiresReceipt = data.requiresReceipt ?? old.requiresReceipt,
                isActive = data.isActive ?? old.isActive,
                description = data.description ?? old.description
            };

            ExpenseTypesCache[idx] = updated;
            return Ok(updated);
        }
    }

    /// <summary>
    /// 删除报销类型
    /// </summary>
    [HttpDelete("types/{id:guid}")]
    [RequirePermission("expense:write")]
    public IActionResult DeleteExpenseType(Guid id)
    {
        lock (ExpenseTypesLock)
        {
            ExpenseTypesCache = ExpenseTypesCache.Where(t => t.id != id).ToList();
        }
        return Ok(new { message = "ok" });
    }

    /// <summary>
    /// 获取报销申请列表
    /// </summary>
    [HttpGet("requests")]
    [RequirePermission("expense:read")]
    public async Task<IActionResult> GetExpenseRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query =
            from r in context.ExpenseRequests
            join e in context.Employees on r.EmployeeId equals e.Id
            select new { r, EmployeeName = e.LastName + e.FirstName, e.EmployeeNumber };

        if (employeeId.HasValue)
        {
            query = query.Where(x => x.r.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.r.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.r.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.r.CreatedAt < endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var raw = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var requestIds = raw.Select(x => x.r.Id).Distinct().ToList();
        var itemsByRequest = await context.Set<Hrevolve.Domain.Expense.ExpenseItem>()
            .Where(i => requestIds.Contains(i.ExpenseRequestId))
            .ToListAsync(cancellationToken);
        var itemsLookup = itemsByRequest
            .GroupBy(i => i.ExpenseRequestId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ExpenseDate).ToList());

        var approvalsByRequest = await context.Set<Hrevolve.Domain.Expense.ExpenseApproval>()
            .Where(a => requestIds.Contains(a.ExpenseRequestId))
            .ToListAsync(cancellationToken);
        var approvalLookup = approvalsByRequest
            .GroupBy(a => a.ExpenseRequestId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ApprovedAt).FirstOrDefault());

        var deptByEmployee = await (
            from j in context.JobHistories
            where raw.Select(x => x.r.EmployeeId).Contains(j.EmployeeId) && j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            join d in context.OrganizationUnits on j.DepartmentId equals d.Id
            select new { j.EmployeeId, d.Name }
        ).ToDictionaryAsync(x => x.EmployeeId, x => x.Name, cancellationToken);

        var types = GetTypesSnapshot();
        var typeByCategory = types.GroupBy(t => t.category).ToDictionary(g => g.Key, g => g.First());
        var typeById = types.ToDictionary(t => t.id, t => t);

        var items = raw.Select(x =>
        {
            itemsLookup.TryGetValue(x.r.Id, out var requestItems);
            var first = requestItems?.FirstOrDefault();
            var cat = first?.Category.ToString() ?? "Other";
            var mappedType = typeByCategory.TryGetValue(cat, out var tt) ? tt : types.First();

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(cat, category, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var receiptUrls = (requestItems ?? [])
                .Where(i => i.ReceiptUrl != null)
                .Select(i => i.ReceiptUrl!)
                .Distinct()
                .ToList();

            approvalLookup.TryGetValue(x.r.Id, out var latestApproval);

            return new
            {
                id = x.r.Id,
                employeeId = x.r.EmployeeId,
                employeeName = x.EmployeeName,
                employeeNo = x.EmployeeNumber,
                departmentName = deptByEmployee.TryGetValue(x.r.EmployeeId, out var dn) ? dn : null,
                expenseTypeId = mappedType.id,
                expenseTypeName = mappedType.name,
                category = cat,
                amount = x.r.TotalAmount,
                currency = "CNY",
                expenseDate = first?.ExpenseDate ?? DateOnly.FromDateTime(x.r.CreatedAt),
                description = x.r.Title,
                receiptUrls,
                status = MapStatus(x.r.Status),
                approverId = latestApproval?.ApproverId,
                approverName = latestApproval != null ? "系统" : null,
                approvedAt = latestApproval?.ApprovedAt,
                approverComment = latestApproval?.Comments,
                createdAt = x.r.CreatedAt
            };
        }).Where(x => x != null).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items, total = totalCount, page, pageSize, totalPages });
    }

    /// <summary>
    /// 创建报销申请
    /// </summary>
    [HttpPost("requests")]
    [RequirePermission("expense:write")]
    public async Task<IActionResult> CreateExpenseRequest([FromBody] CreateExpenseRequestDto data, CancellationToken cancellationToken)
    {
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        var tenantId = currentUserAccessor.CurrentUser?.TenantId;
        if (!employeeId.HasValue || !tenantId.HasValue) return Unauthorized();

        var typeId = Guid.Parse(data.expenseTypeId);
        var mapped = GetTypesSnapshot().FirstOrDefault(t => t.id == typeId);
        if (mapped == null) return BadRequest();

        var req = Hrevolve.Domain.Expense.ExpenseRequest.Create(
            tenantId.Value,
            employeeId.Value,
            data.description);

        var date = DateOnly.Parse(data.expenseDate);
        var receiptUrl = data.items?.FirstOrDefault()?.receiptUrl;

        req.AddItem(ParseCategory(mapped.category), data.amount, date, data.description, receiptUrl);
        req.Submit();

        await context.ExpenseRequests.AddAsync(req, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = req.Id,
            employeeId = req.EmployeeId,
            expenseTypeId = mapped.id,
            category = mapped.category,
            amount = req.TotalAmount,
            currency = data.currency ?? "CNY",
            expenseDate = date,
            description = req.Title,
            receiptUrls = req.Items.Where(i => i.ReceiptUrl != null).Select(i => i.ReceiptUrl!).Distinct().ToList(),
            status = MapStatus(req.Status),
            createdAt = req.CreatedAt
        });
    }

    /// <summary>
    /// 审批报销申请
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    [RequirePermission("expense:approve")]
    public async Task<IActionResult> ApproveExpenseRequest(Guid id, [FromBody] ApprovalDecision data, CancellationToken cancellationToken)
    {
        var approverId = currentUserAccessor.CurrentUser?.Id ?? Guid.Empty;
        var req = await context.ExpenseRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (req == null) return NotFound();

        if (req.Status != Hrevolve.Domain.Expense.ExpenseRequestStatus.Pending) return Ok(new { message = "ok" });

        if (data.approved)
        {
            req.Approve(approverId, data.comment);
            var approval = req.Approvals.LastOrDefault();
            if (approval != null)
            {
                context.Entry(approval).State = EntityState.Added;
            }
        }
        else
        {
            req.Reject(approverId, data.comment ?? "rejected");
            var approval = req.Approvals.LastOrDefault();
            if (approval != null)
            {
                context.Entry(approval).State = EntityState.Added;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "ok" });
    }

    private static IReadOnlyList<ExpenseTypeDto> GetTypesSnapshot()
    {
        lock (ExpenseTypesLock)
        {
            return ExpenseTypesCache.ToList();
        }
    }

    private static string MapStatus(Hrevolve.Domain.Expense.ExpenseRequestStatus status) =>
        status switch
        {
            Hrevolve.Domain.Expense.ExpenseRequestStatus.Draft => "Pending",
            Hrevolve.Domain.Expense.ExpenseRequestStatus.Pending => "Pending",
            Hrevolve.Domain.Expense.ExpenseRequestStatus.Approved => "Approved",
            Hrevolve.Domain.Expense.ExpenseRequestStatus.Rejected => "Rejected",
            Hrevolve.Domain.Expense.ExpenseRequestStatus.Paid => "Paid",
            _ => "Pending"
        };

    private static List<ExpenseTypeDto> DemoTypes()
    {
        var defs = Hrevolve.Infrastructure.Persistence.DemoDataSeeder.GetExpenseTypeDefinitions();
        return defs.Select(d => new ExpenseTypeDto(d.Id, d.Code, d.Name, d.Category.ToString(), d.MaxAmount, d.RequiresReceipt, true, null)).ToList();
    }

    private static Hrevolve.Domain.Expense.ExpenseCategory ParseCategory(string category) =>
        Enum.Parse<Hrevolve.Domain.Expense.ExpenseCategory>(category, ignoreCase: true);

    public record ExpenseTypeDto(Guid id, string code, string name, string category, decimal? maxAmount, bool requiresReceipt, bool isActive, string? description);

    public record ExpenseTypeUpsert(string? code, string? name, string? category, decimal? maxAmount, bool? requiresReceipt, bool? isActive, string? description);

    public record CreateExpenseRequestDto(string expenseTypeId, decimal amount, string expenseDate, string description, string? currency, List<CreateExpenseItemDto>? items);

    public record CreateExpenseItemDto(string? receiptUrl);

    public record ApprovalDecision(bool approved, string? comment);
}
