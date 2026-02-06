namespace Hrevolve.Web.Controllers;

/// <summary>
/// 假期管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController(
    IMediator mediator,
    Hrevolve.Infrastructure.Persistence.HrevolveDbContext context,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
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
    /// 获取请假申请列表（管理员）
    /// </summary>
    [HttpGet("requests")]
    [RequirePermission(Permissions.LeaveRead)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query =
            from r in context.LeaveRequests
            join e in context.Employees on r.EmployeeId equals e.Id
            join t in context.LeaveTypes on r.LeaveTypeId equals t.Id
            select new { r, EmployeeName = e.LastName + e.FirstName, EmployeeNo = e.EmployeeNumber, LeaveTypeName = t.Name, LeaveTypeColor = t.Color };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.r.Status.ToString() == status);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(x => x.r.EmployeeId == employeeId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var itemsRaw = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var requestIds = itemsRaw.Select(x => x.r.Id).Distinct().ToList();
        var approvals = await context.Set<Hrevolve.Domain.Leave.LeaveApproval>()
            .Where(a => requestIds.Contains(a.LeaveRequestId))
            .ToListAsync(cancellationToken);
        var approvalLookup = approvals
            .GroupBy(a => a.LeaveRequestId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ApprovedAt).FirstOrDefault());

        var departmentLookup = await GetDepartmentNamesAsync(itemsRaw.Select(x => x.r.EmployeeId).Distinct().ToList(), cancellationToken);

        var items = itemsRaw.Select(x =>
        {
            approvalLookup.TryGetValue(x.r.Id, out var approval);
            return new
            {
                id = x.r.Id,
                employeeId = x.r.EmployeeId,
                employeeName = x.EmployeeName,
                employeeNo = x.EmployeeNo,
                departmentName = departmentLookup.TryGetValue(x.r.EmployeeId, out var dn) ? dn : null,
                leaveTypeId = x.r.LeaveTypeId,
                leaveTypeName = x.LeaveTypeName,
                leaveTypeColor = x.LeaveTypeColor,
                startDate = x.r.StartDate,
                endDate = x.r.EndDate,
                days = x.r.TotalDays,
                reason = x.r.Reason,
                attachments = ParseAttachmentUrls(x.r.Attachments),
                status = x.r.Status.ToString(),
                approverId = approval?.ApproverId,
                approverName = approval != null ? "系统" : null,
                approvedAt = approval?.ApprovedAt,
                approverComment = approval?.Comments,
                createdAt = x.r.CreatedAt
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items, total = totalCount, page, pageSize, totalPages });
    }
    
    /// <summary>
    /// 获取待审批的请假申请
    /// </summary>
    [HttpGet("requests/pending")]
    [RequirePermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> GetPendingLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetLeaveRequests(page, pageSize, status: LeaveRequestStatus.Pending.ToString(), employeeId: null, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// 获取请假申请详情
    /// </summary>
    [HttpGet("requests/{id:guid}")]
    [RequirePermission(Permissions.LeaveRead)]
    public async Task<IActionResult> GetLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await (from r in context.LeaveRequests
            where r.Id == id
            join e in context.Employees on r.EmployeeId equals e.Id
            join t in context.LeaveTypes on r.LeaveTypeId equals t.Id
            select new { r, EmployeeName = e.LastName + e.FirstName, EmployeeNo = e.EmployeeNumber, LeaveTypeName = t.Name, LeaveTypeColor = t.Color })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null) return NotFound();

        var departmentLookup = await GetDepartmentNamesAsync([result.r.EmployeeId], cancellationToken);
        var approval = await context.Set<Hrevolve.Domain.Leave.LeaveApproval>()
            .Where(a => a.LeaveRequestId == result.r.Id)
            .OrderByDescending(a => a.ApprovedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            id = result.r.Id,
            employeeId = result.r.EmployeeId,
            employeeName = result.EmployeeName,
            employeeNo = result.EmployeeNo,
            departmentName = departmentLookup.TryGetValue(result.r.EmployeeId, out var dn) ? dn : null,
            leaveTypeId = result.r.LeaveTypeId,
            leaveTypeName = result.LeaveTypeName,
            leaveTypeColor = result.LeaveTypeColor,
            startDate = result.r.StartDate,
            endDate = result.r.EndDate,
            days = result.r.TotalDays,
            reason = result.r.Reason,
            attachments = ParseAttachmentUrls(result.r.Attachments),
            status = result.r.Status.ToString(),
            approverId = approval?.ApproverId,
            approverName = approval != null ? "系统" : null,
            approvedAt = approval?.ApprovedAt,
            approverComment = approval?.Comments,
            createdAt = result.r.CreatedAt
        });
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
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue)
        {
            return Ok(new { items = Array.Empty<object>(), total = 0, page = pageNumber, pageSize, totalPages = 0 });
        }

        return await GetLeaveRequests(pageNumber, pageSize, status: null, employeeId: employeeId.Value, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// 审批请假申请
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    [RequirePermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> ApproveLeaveRequest(
        Guid id,
        [FromBody] LeaveApprovalDecision request,
        CancellationToken cancellationToken)
    {
        var approverId = currentUserAccessor.CurrentUser?.Id ?? Guid.Empty;
        var leaveRequest = await context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (leaveRequest == null) return NotFound();

        if (leaveRequest.Status != LeaveRequestStatus.Pending) return Ok();

        var year = leaveRequest.StartDate.Year;
        var balance = await context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId && b.LeaveTypeId == leaveRequest.LeaveTypeId && b.Year == year, cancellationToken);

        if (request.approved)
        {
            leaveRequest.Approve(approverId, request.comment);
            var approval = leaveRequest.Approvals.LastOrDefault();
            if (approval != null)
            {
                context.Entry(approval).State = EntityState.Added;
            }
            if (balance != null)
            {
                balance.Use(leaveRequest.TotalDays);
            }
            await context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "approved" });
        }

        leaveRequest.Reject(approverId, request.comment ?? "rejected");
        var rejectedApproval = leaveRequest.Approvals.LastOrDefault();
        if (rejectedApproval != null)
        {
            context.Entry(rejectedApproval).State = EntityState.Added;
        }
        if (balance != null)
        {
            balance.RemovePending(leaveRequest.TotalDays);
        }
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "rejected" });
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
        return await ApproveLeaveRequest(id, new LeaveApprovalDecision(false, request.Reason), cancellationToken);
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
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue) return Unauthorized();

        var leaveRequest = await context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == employeeId.Value, cancellationToken);
        if (leaveRequest == null) return NotFound();

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            leaveRequest.Cancel(request.Reason);
            await context.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        var year = leaveRequest.StartDate.Year;
        var balance = await context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == leaveRequest.EmployeeId && b.LeaveTypeId == leaveRequest.LeaveTypeId && b.Year == year, cancellationToken);

        leaveRequest.Cancel(request.Reason);
        if (balance != null)
        {
            balance.RemovePending(leaveRequest.TotalDays);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// 获取员工假期余额
    /// </summary>
    [HttpGet("balances/{employeeId:guid}")]
    [RequirePermission(Permissions.LeaveRead)]
    public async Task<IActionResult> GetEmployeeLeaveBalances(
        Guid employeeId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var y = year ?? DateTime.Today.Year;
        var balances = await GetLeaveBalancesAsync(employeeId, y, cancellationToken);
        return Ok(balances);
    }
    
    /// <summary>
    /// 获取我的假期余额
    /// </summary>
    [HttpGet("balances/my")]
    public async Task<IActionResult> GetMyLeaveBalances(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue) return Ok(Array.Empty<object>());
        var y = year ?? DateTime.Today.Year;
        var balances = await GetLeaveBalancesAsync(employeeId.Value, y, cancellationToken);
        return Ok(balances);
    }
    
    /// <summary>
    /// 获取假期类型列表
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken cancellationToken)
    {
        var types = await context.LeaveTypes
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var policies = await context.LeavePolicies
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var policyByType = policies
            .GroupBy(p => p.LeaveTypeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.BaseQuota).First());

        var items = types.Select(t =>
        {
            policyByType.TryGetValue(t.Id, out var policy);
            return new
            {
                id = t.Id,
                code = t.Code,
                name = t.Name,
                description = t.Description,
                isPaid = t.IsPaid,
                defaultDays = policy?.BaseQuota ?? 0m,
                maxCarryOver = policy?.MaxCarryOverDays ?? 0m,
                carryOverExpiry = policy?.CarryOverExpiryMonths,
                requiresApproval = t.RequiresApproval,
                allowHalfDay = t.AllowHalfDay,
                color = t.Color ?? "#999999",
                isActive = t.IsActive
            };
        }).ToList();

        return Ok(items);
    }

    private async Task<Dictionary<Guid, string>> GetDepartmentNamesAsync(List<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var jobs =
            from j in context.JobHistories
            where employeeIds.Contains(j.EmployeeId) && j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            join d in context.OrganizationUnits on j.DepartmentId equals d.Id
            select new { j.EmployeeId, DepartmentName = d.Name };

        return await jobs.ToDictionaryAsync(x => x.EmployeeId, x => x.DepartmentName, cancellationToken);
    }

    private async Task<List<object>> GetLeaveBalancesAsync(Guid employeeId, int year, CancellationToken cancellationToken)
    {
        var balances = await context.LeaveBalances
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync(cancellationToken);

        var types = await context.LeaveTypes.ToDictionaryAsync(t => t.Id, t => t, cancellationToken);

        return balances.Select(b =>
        {
            types.TryGetValue(b.LeaveTypeId, out var t);
            var totalDays = b.Entitlement + b.CarriedOver;
            return new
            {
                id = b.Id,
                employeeId = b.EmployeeId,
                leaveTypeId = b.LeaveTypeId,
                leaveTypeName = t?.Name ?? string.Empty,
                leaveTypeColor = t?.Color,
                year = b.Year,
                totalDays,
                usedDays = b.Used,
                pendingDays = b.Pending,
                remainingDays = b.Available,
                carryOverDays = b.CarriedOver
            };
        }).Cast<object>().ToList();
    }

    private static List<string> ParseAttachmentUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            return parsed ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public record LeaveApprovalDecision(bool approved, string? comment);
public record RejectLeaveRequest(string Reason);
public record CancelLeaveRequest(string Reason);
