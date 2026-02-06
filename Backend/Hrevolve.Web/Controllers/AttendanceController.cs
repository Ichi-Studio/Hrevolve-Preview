namespace Hrevolve.Web.Controllers;

/// <summary>
/// 考勤管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController(
    Hrevolve.Infrastructure.Persistence.HrevolveDbContext context,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    
    /// <summary>
    /// 签到
    /// </summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现签到命令
        return Ok(new 
        { 
            message = "签到成功", 
            time = DateTime.UtcNow,
            method = request.Method
        });
    }
    
    /// <summary>
    /// 签退
    /// </summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut(
        [FromBody] CheckOutRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现签退命令
        return Ok(new 
        { 
            message = "签退成功", 
            time = DateTime.UtcNow,
            method = request.Method
        });
    }
    
    /// <summary>
    /// 补卡申请
    /// </summary>
    [HttpPost("correction")]
    public async Task<IActionResult> ApplyCorrection(
        [FromBody] CorrectionRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: 实现补卡命令
        return Ok(new { message = "补卡申请已提交" });
    }
    
    /// <summary>
    /// 获取考勤记录列表（管理员）
    /// </summary>
    [HttpGet("records")]
    [RequirePermission(Permissions.AttendanceRead)]
    public async Task<IActionResult> GetAttendanceRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var rangeStart = startDate ?? DateOnly.FromDateTime(DateTime.Today).AddDays(-30);
        var rangeEnd = endDate ?? DateOnly.FromDateTime(DateTime.Today);

        var currentJobs =
            from j in context.JobHistories
            where j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null
            select new { j.EmployeeId, j.DepartmentId };

        var baseQuery =
            from r in context.AttendanceRecords
            where r.AttendanceDate >= rangeStart && r.AttendanceDate <= rangeEnd
            join e in context.Employees on r.EmployeeId equals e.Id
            join cj in currentJobs on e.Id equals cj.EmployeeId
            join dept in context.OrganizationUnits on cj.DepartmentId equals dept.Id
            join s in context.Schedules on r.ScheduleId equals s.Id into sched
            from schedItem in sched.DefaultIfEmpty()
            join sh in context.Shifts on schedItem.ShiftId equals sh.Id into shj
            from shift in shj.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = e.LastName + e.FirstName,
                EmployeeNo = e.EmployeeNumber,
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                r.AttendanceDate,
                r.CheckInTime,
                r.CheckOutTime,
                r.CheckInLocation,
                r.CheckOutLocation,
                r.OvertimeHours,
                r.Remarks,
                ShiftId = (Guid?)shift.Id,
                ShiftName = shift.Name,
                ShiftStart = shift.StartTime,
                ShiftEnd = shift.EndTime,
                ShiftCrossDay = shift.CrossDay
            };

        if (employeeId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.DepartmentId == departmentId.Value);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var pageItems = await baseQuery
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var requestLeaveRanges = await context.LeaveRequests
            .Where(l => l.Status == Hrevolve.Domain.Leave.LeaveRequestStatus.Approved)
            .Where(l => l.StartDate <= rangeEnd && l.EndDate >= rangeStart)
            .Select(l => new { l.EmployeeId, l.StartDate, l.EndDate })
            .ToListAsync(cancellationToken);

        var items = pageItems.Select(x =>
        {
            var isLeave = requestLeaveRanges.Any(l => l.EmployeeId == x.EmployeeId && x.AttendanceDate >= l.StartDate && x.AttendanceDate <= l.EndDate);
            var computed = ComputeStatus(x.AttendanceDate, x.CheckInTime, x.CheckOutTime, x.ShiftStart, x.ShiftEnd, x.ShiftCrossDay, isLeave);

            return new
            {
                id = x.Id,
                employeeId = x.EmployeeId,
                employeeName = x.EmployeeName,
                employeeNo = x.EmployeeNo,
                departmentName = x.DepartmentName,
                date = x.AttendanceDate,
                shiftId = x.ShiftId,
                shiftName = x.ShiftName,
                checkInTime = x.CheckInTime,
                checkOutTime = x.CheckOutTime,
                checkInLocation = x.CheckInLocation,
                checkOutLocation = x.CheckOutLocation,
                status = computed.Status,
                workHours = computed.WorkHours,
                overtimeHours = (double)x.OvertimeHours,
                lateMinutes = computed.LateMinutes,
                earlyLeaveMinutes = computed.EarlyLeaveMinutes,
                remark = x.Remarks
            };
        }).ToList();

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
    /// 获取我的考勤记录
    /// </summary>
    [HttpGet("records/my")]
    public async Task<IActionResult> GetMyAttendanceRecords(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue) return Ok(new { items = Array.Empty<object>(), total = 0, page = pageNumber, pageSize, totalPages = 0 });

        return await GetAttendanceRecords(
            pageNumber,
            pageSize,
            employeeId: employeeId.Value,
            departmentId: null,
            startDate: startDate,
            endDate: endDate,
            status: null,
            cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// 获取今日考勤状态
    /// </summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayAttendance(CancellationToken cancellationToken)
    {
        // TODO: 实现获取今日考勤状态查询
        return Ok(new { message = "获取今日考勤状态功能待实现" });
    }
    
    /// <summary>
    /// 获取月度考勤统计
    /// </summary>
    [HttpGet("stats/monthly")]
    public async Task<IActionResult> GetMonthlyStats(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserAccessor.CurrentUser?.EmployeeId;
        if (!employeeId.HasValue)
        {
            return Ok(new { workDays = 0, attendedDays = 0, lateDays = 0, earlyLeaveDays = 0, absentDays = 0, leaveDays = 0, overtimeHours = 0 });
        }

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var schedules = await context.Schedules
            .Where(s => s.EmployeeId == employeeId.Value && s.ScheduleDate >= start && s.ScheduleDate <= end)
            .ToListAsync(cancellationToken);

        var shifts = await context.Shifts.ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        var records = await context.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId.Value && r.AttendanceDate >= start && r.AttendanceDate <= end)
            .ToListAsync(cancellationToken);

        var leaveRanges = await context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId.Value && l.Status == Hrevolve.Domain.Leave.LeaveRequestStatus.Approved)
            .Select(l => new { l.StartDate, l.EndDate })
            .ToListAsync(cancellationToken);

        var daySet = schedules.Where(s => !s.IsRestDay).Select(s => s.ScheduleDate).Distinct().ToHashSet();
        if (daySet.Count == 0)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) daySet.Add(d);
            }
        }

        var byDate = records.ToDictionary(r => r.AttendanceDate, r => r);
        var late = 0;
        var early = 0;
        var attended = 0;
        var absent = 0;
        var leave = 0;
        double overtime = 0;

        foreach (var date in daySet)
        {
            var isLeave = leaveRanges.Any(l => date >= l.StartDate && date <= l.EndDate);
            if (isLeave)
            {
                leave++;
                continue;
            }

            if (!byDate.TryGetValue(date, out var record))
            {
                absent++;
                continue;
            }

            var schedule = schedules.FirstOrDefault(s => s.ScheduleDate == date && !s.IsRestDay);
            shifts.TryGetValue(schedule?.ShiftId ?? Guid.Empty, out var shift);
            var computed = ComputeStatus(date, record.CheckInTime, record.CheckOutTime, shift?.StartTime, shift?.EndTime, shift?.CrossDay ?? false, false);

            if (computed.Status == "Absent") absent++;
            else attended++;

            if (computed.Status == "Late") late++;
            if (computed.Status == "EarlyLeave") early++;

            overtime += (double)record.OvertimeHours;
        }

        return Ok(new
        {
            workDays = daySet.Count,
            attendedDays = attended,
            lateDays = late,
            earlyLeaveDays = early,
            absentDays = absent,
            leaveDays = leave,
            overtimeHours = Math.Round(overtime, 2)
        });
    }
    
    /// <summary>
    /// 获取班次列表
    /// </summary>
    [HttpGet("shifts")]
    [RequirePermission(Permissions.AttendanceRead)]
    public async Task<IActionResult> GetShifts(CancellationToken cancellationToken)
    {
        var items = await context.Shifts
            .OrderBy(s => s.Code)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                code = s.Code,
                startTime = s.StartTime.ToString("HH:mm"),
                endTime = s.EndTime.ToString("HH:mm"),
                breakMinutes = s.BreakStartTime.HasValue && s.BreakEndTime.HasValue ? (int)(s.BreakEndTime.Value.ToTimeSpan() - s.BreakStartTime.Value.ToTimeSpan()).TotalMinutes : 0,
                isFlexible = s.FlexibleStartMinutes > 0 || s.FlexibleEndMinutes > 0,
                flexibleMinutes = Math.Max(s.FlexibleStartMinutes, s.FlexibleEndMinutes)
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
    
    /// <summary>
    /// 获取部门考勤统计
    /// </summary>
    [HttpGet("statistics/department/{departmentId:guid}")]
    [RequirePermission(Permissions.AttendanceRead)]
    public async Task<IActionResult> GetDepartmentAttendanceStatistics(
        Guid departmentId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        // TODO: 实现部门考勤统计查询
        return Ok(new { message = "获取部门考勤统计功能待实现" });
    }
    
    /// <summary>
    /// 审批补卡申请
    /// </summary>
    [HttpPost("correction/{id:guid}/approve")]
    [RequirePermission(Permissions.AttendanceApprove)]
    public async Task<IActionResult> ApproveCorrection(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: 实现审批补卡命令
        return Ok(new { message = "补卡审批功能待实现" });
    }

    private static (string Status, double? WorkHours, int? LateMinutes, int? EarlyLeaveMinutes) ComputeStatus(
        DateOnly date,
        DateTime? checkInUtc,
        DateTime? checkOutUtc,
        TimeOnly? shiftStart,
        TimeOnly? shiftEnd,
        bool crossDay,
        bool isLeave)
    {
        if (isLeave) return ("Leave", null, null, null);

        if (!checkInUtc.HasValue && !checkOutUtc.HasValue) return ("Absent", null, null, null);
        if (!checkInUtc.HasValue || !checkOutUtc.HasValue) return ("Absent", null, null, null);

        var workHours = (checkOutUtc.Value - checkInUtc.Value).TotalHours;
        if (workHours < 0) workHours = 0;

        if (!shiftStart.HasValue || !shiftEnd.HasValue) return ("Normal", Math.Round(workHours, 2), null, null);

        var localDay = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var schedStart = localDay.AddHours(shiftStart.Value.Hour).AddMinutes(shiftStart.Value.Minute);
        var schedEnd = localDay.AddHours(shiftEnd.Value.Hour).AddMinutes(shiftEnd.Value.Minute);
        if (crossDay || shiftEnd.Value < shiftStart.Value) schedEnd = schedEnd.AddDays(1);

        var checkInLocal = checkInUtc.Value.ToLocalTime();
        var checkOutLocal = checkOutUtc.Value.ToLocalTime();

        var grace = TimeSpan.FromMinutes(10);
        var lateMinutes = (int)Math.Max(0, (checkInLocal - (schedStart + grace)).TotalMinutes);
        var earlyMinutes = (int)Math.Max(0, ((schedEnd - grace) - checkOutLocal).TotalMinutes);

        if (lateMinutes > 0) return ("Late", Math.Round(workHours, 2), lateMinutes, null);
        if (earlyMinutes > 0) return ("EarlyLeave", Math.Round(workHours, 2), null, earlyMinutes);
        return ("Normal", Math.Round(workHours, 2), null, null);
    }
}

public record CheckInRequest(
    CheckInMethod Method,
    string? Location,
    string? WifiSsid);

public record CheckOutRequest(
    CheckInMethod Method,
    string? Location,
    string? WifiSsid);

public record CorrectionRequest(
    DateOnly Date,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    string Reason);
