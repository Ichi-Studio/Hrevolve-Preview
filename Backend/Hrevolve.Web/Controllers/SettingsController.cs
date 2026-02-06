using System.Security.Claims;
using Hrevolve.Domain.Attendance;
using Hrevolve.Domain.Employees;
using Hrevolve.Domain.Organizations;
using Hrevolve.Web.Filters;

namespace Hrevolve.Web.Controllers;

/// <summary>
/// 系统设置控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController(Hrevolve.Infrastructure.Persistence.HrevolveDbContext context) : ControllerBase
{
    /// <summary>
    /// 获取系统配置
    /// </summary>
    [HttpGet("system-configs")]
    public IActionResult GetSystemConfigs()
    {
        return Ok(new
        {
            general = new { systemName = "Hrevolve", companyName = "演示公司", timezone = "Asia/Shanghai", dateFormat = "YYYY-MM-DD", language = "zh-CN", currency = "CNY" },
            security = new { passwordMinLength = 8, passwordRequireUppercase = true, passwordRequireLowercase = true, passwordRequireNumber = true, passwordRequireSpecial = false, sessionTimeout = 30, maxLoginAttempts = 5, lockoutDuration = 15, enableTwoFactor = false },
            notification = new { enableEmail = true, enableSms = false, enablePush = true, emailServer = "", emailPort = 587, emailUsername = "" }
        });
    }

    /// <summary>
    /// 更新系统配置
    /// </summary>
    [HttpPut("system-configs")]
    public IActionResult UpdateSystemConfigs([FromBody] object data)
    {
        return Ok(new { message = "保存成功" });
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);

        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(u =>
                u.Username.Contains(kw) ||
                u.Email.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToLowerInvariant();
            if (normalized is "enabled" or "active")
            {
                query = query.Where(u => u.Status == UserStatus.Active);
            }
            else if (normalized is "disabled" or "inactive")
            {
                query = query.Where(u => u.Status == UserStatus.Inactive);
            }
            else if (Enum.TryParse<UserStatus>(status, true, out var parsed))
            {
                query = query.Where(u => u.Status == parsed);
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleCode = role.Trim();
            var roleId = await context.Roles
                .Where(r => r.Code == roleCode)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleId == Guid.Empty)
            {
                return Ok(new { items = Array.Empty<object>(), total = 0, page, pageSize });
            }

            query = query.Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Phone,
                u.Status,
                u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToArray();
        var roleRows = await (
            from ur in context.UserRoles.AsNoTracking()
            where userIds.Contains(ur.UserId)
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select new { ur.UserId, r.Code }
        ).ToListAsync(cancellationToken);

        var rolesByUserId = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        var items = users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            displayName = u.Username,
            email = u.Email,
            phone = u.Phone,
            roles = rolesByUserId.TryGetValue(u.Id, out var rolesForUser) ? rolesForUser : Array.Empty<string>(),
            isActive = u.Status == UserStatus.Active,
            lastLoginTime = u.LastLoginAt
        });

        return Ok(new { items, total, page, pageSize });
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();

        var roles = await (
            from ur in context.UserRoles.AsNoTracking()
            where ur.UserId == id
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select r.Code
        ).Distinct().ToListAsync(cancellationToken);

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            displayName = user.Username,
            email = user.Email,
            phone = user.Phone,
            roles,
            isActive = user.Status == UserStatus.Active,
            lastLoginTime = user.LastLoginAt
        });
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest data, CancellationToken cancellationToken = default)
    {
        var username = data.Username.Trim();
        var email = data.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username)) return BadRequest(new { message = "用户名不能为空" });
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { message = "邮箱不能为空" });

        var exists = await context.Users.AnyAsync(u => u.Username == username || u.Email == email.ToLowerInvariant(), cancellationToken);
        if (exists) return BadRequest(new { message = "用户名或邮箱已存在" });

        var user = Hrevolve.Domain.Identity.User.Create(Guid.Empty, username, email);
        user.SetPhone(data.Phone);
        user.SetPassword(string.IsNullOrWhiteSpace(data.Password) ? "123456" : data.Password);
        user.SetStatus(data.IsActive == false ? UserStatus.Inactive : UserStatus.Active);

        if (data.Roles is { Count: > 0 })
        {
            var roleCodes = data.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var roleIdMap = await context.Roles
                .Where(r => roleCodes.Contains(r.Code))
                .Select(r => new { r.Code, r.Id })
                .ToListAsync(cancellationToken);

            var missing = roleCodes.Except(roleIdMap.Select(x => x.Code), StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0) return BadRequest(new { message = $"角色不存在: {string.Join(", ", missing)}" });

            foreach (var roleId in roleIdMap.Select(x => x.Id))
            {
                user.AddRole(roleId);
            }
        }

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { id = user.Id, message = "创建成功" });
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] JsonElement data, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();

        if (data.ValueKind != JsonValueKind.Object) return BadRequest(new { message = "请求体格式不正确" });

        if (data.TryGetProperty("username", out var usernameEl) && usernameEl.ValueKind == JsonValueKind.String)
        {
            user.SetUsername(usernameEl.GetString()!);
        }

        if (data.TryGetProperty("email", out var emailEl) && emailEl.ValueKind == JsonValueKind.String)
        {
            user.SetEmail(emailEl.GetString()!);
        }

        if (data.TryGetProperty("phone", out var phoneEl))
        {
            if (phoneEl.ValueKind == JsonValueKind.Null) user.SetPhone(null);
            else if (phoneEl.ValueKind == JsonValueKind.String) user.SetPhone(phoneEl.GetString());
        }

        if (data.TryGetProperty("isActive", out var isActiveEl) && isActiveEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            user.SetStatus(isActiveEl.GetBoolean() ? UserStatus.Active : UserStatus.Inactive);
        }

        if (data.TryGetProperty("roles", out var rolesEl))
        {
            if (rolesEl.ValueKind == JsonValueKind.Null)
            {
                var existing = await context.UserRoles.Where(ur => ur.UserId == id).ToListAsync(cancellationToken);
                context.UserRoles.RemoveRange(existing);
            }
            else if (rolesEl.ValueKind == JsonValueKind.Array)
            {
                var roleCodes = rolesEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var roleIds = await context.Roles
                    .Where(r => roleCodes.Contains(r.Code))
                    .Select(r => new { r.Code, r.Id })
                    .ToListAsync(cancellationToken);

                var missing = roleCodes.Except(roleIds.Select(x => x.Code), StringComparer.OrdinalIgnoreCase).ToArray();
                if (missing.Length > 0) return BadRequest(new { message = $"角色不存在: {string.Join(", ", missing)}" });

                var desiredRoleIds = roleIds.Select(x => x.Id).ToHashSet();
                var existing = await context.UserRoles.Where(ur => ur.UserId == id).ToListAsync(cancellationToken);
                var existingRoleIds = existing.Select(e => e.RoleId).ToHashSet();

                var toRemove = existing.Where(e => !desiredRoleIds.Contains(e.RoleId)).ToList();
                if (toRemove.Count > 0) context.UserRoles.RemoveRange(toRemove);

                var toAdd = desiredRoleIds.Except(existingRoleIds).ToArray();
                foreach (var roleId in toAdd)
                {
                    await context.UserRoles.AddAsync(new UserRole(id, roleId), cancellationToken);
                }
            }
            else
            {
                return BadRequest(new { message = "roles 字段格式不正确" });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    [HttpPost("users/{id:guid}/disable")]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();
        user.SetStatus(UserStatus.Inactive);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "已禁用" });
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    [HttpPost("users/{id:guid}/enable")]
    public async Task<IActionResult> EnableUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();
        user.SetStatus(UserStatus.Active);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "已启用" });
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();
        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword) ? "123456" : request.NewPassword;
        user.SetPassword(newPassword);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "密码已重置" });
    }

    [HttpPost("users/me/create-ceo-employee-and-bind")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> CreateCeoEmployeeAndBind(CancellationToken cancellationToken = default)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return Unauthorized();

        var tenantId = user.TenantId != Guid.Empty ? user.TenantId : context.CurrentTenantId;
        if (tenantId == Guid.Empty) return BadRequest(new { message = "无法识别租户信息" });

        var company = await context.OrganizationUnits
            .OrderBy(x => x.Level)
            .FirstOrDefaultAsync(x => x.Code == "COMP" || x.Type == OrganizationUnitType.Company, cancellationToken);
        if (company == null) return BadRequest(new { message = "未找到公司组织（COMP）" });

        var ceoPosition = await context.Positions
            .FirstOrDefaultAsync(p => p.Code == "CEO", cancellationToken);
        if (ceoPosition == null) return BadRequest(new { message = "未找到 CEO 职位（Code=CEO）" });

        var employeeNumber = await GenerateUniqueEmployeeNumberAsync("CEO", cancellationToken);
        var hireDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-3);

        var employee = Employee.Create(
            tenantId,
            employeeNumber,
            "天成",
            "赵",
            Gender.Male,
            new DateOnly(1986, 5, 12),
            hireDate,
            EmploymentType.FullTime);

        employee.SetContactInfo($"{employeeNumber.ToLowerInvariant()}@hrevolve.com", "13800009999", null, "上海市黄浦区演示路CEO号");
        employee.LinkUser(user.Id);

        var job = JobHistory.Create(
            tenantId,
            employee.Id,
            ceoPosition.Id,
            company.Id,
            100000m,
            hireDate,
            JobChangeType.NewHire,
            "创建CEO演示员工并绑定账号");

        await context.Employees.AddAsync(employee, cancellationToken);
        await context.JobHistories.AddAsync(job, cancellationToken);

        user.LinkEmployee(employee.Id);

        await EnsureRecentAttendanceAsync(tenantId, employee.Id, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            employeeId = employee.Id,
            employeeNo = employee.EmployeeNumber,
            employeeName = employee.LastName + employee.FirstName,
            boundUser = user.Username
        });
    }

    private async Task<string> GenerateUniqueEmployeeNumberAsync(string prefix, CancellationToken cancellationToken)
    {
        prefix = string.IsNullOrWhiteSpace(prefix) ? "EMP" : prefix.Trim().ToUpperInvariant();
        var seq = 1;
        while (true)
        {
            var candidate = $"{prefix}{seq:0000}";
            var exists = await context.Employees.AnyAsync(e => e.EmployeeNumber == candidate, cancellationToken);
            if (!exists) return candidate;
            seq++;
            if (seq > 9999) throw new InvalidOperationException("无法生成唯一员工工号");
        }
    }

    private async Task EnsureRecentAttendanceAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken)
    {
        var shift = await context.Shifts.FirstOrDefaultAsync(s => s.Code == "DAY", cancellationToken)
                    ?? await context.Shifts.OrderBy(s => s.Code).FirstOrDefaultAsync(cancellationToken);
        if (shift == null) return;

        var start = DateOnly.FromDateTime(DateTime.Today).AddDays(-14);
        var end = DateOnly.FromDateTime(DateTime.Today);

        var existingDates = await context.Schedules
            .Where(s => s.EmployeeId == employeeId && s.ScheduleDate >= start && s.ScheduleDate <= end)
            .Select(s => s.ScheduleDate)
            .ToListAsync(cancellationToken);
        var existingSet = existingDates.ToHashSet();

        var schedulesToAdd = new List<Schedule>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (existingSet.Contains(d)) continue;
            var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            schedulesToAdd.Add(Schedule.Create(tenantId, employeeId, shift.Id, d, isWeekend));
        }

        if (schedulesToAdd.Count > 0)
        {
            await context.Schedules.AddRangeAsync(schedulesToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var schedules = await context.Schedules
            .Where(s => s.EmployeeId == employeeId && s.ScheduleDate >= start && s.ScheduleDate <= end && !s.IsRestDay)
            .ToListAsync(cancellationToken);

        var existingRecordDates = await context.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId && r.AttendanceDate >= start && r.AttendanceDate <= end)
            .Select(r => r.AttendanceDate)
            .ToListAsync(cancellationToken);
        var recordSet = existingRecordDates.ToHashSet();

        var random = new Random();
        var recordsToAdd = new List<AttendanceRecord>();

        foreach (var s in schedules)
        {
            if (recordSet.Contains(s.ScheduleDate)) continue;

            var record = AttendanceRecord.Create(tenantId, employeeId, s.ScheduleDate, s.Id);

            var localDate = DateTime.SpecifyKind(s.ScheduleDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
            var workStart = localDate.AddHours(shift.StartTime.Hour).AddMinutes(shift.StartTime.Minute);
            var workEnd = localDate.AddHours(shift.EndTime.Hour).AddMinutes(shift.EndTime.Minute);
            if (shift.CrossDay) workEnd = workEnd.AddDays(1);

            var checkIn = workStart.AddMinutes(random.Next(-5, 21));
            var checkOut = workEnd.AddMinutes(random.Next(-15, 61));

            record.CheckIn(checkIn.ToUniversalTime(), CheckInMethod.Web, "31.2304,121.4737");
            record.CheckOut(checkOut.ToUniversalTime(), CheckInMethod.Web, "31.2304,121.4737");

            recordsToAdd.Add(record);
        }

        if (recordsToAdd.Count > 0)
        {
            await context.AttendanceRecords.AddRangeAsync(recordsToAdd, cancellationToken);
        }
    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.IsSystemRole
            })
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToArray();
        var permRows = await context.Set<RolePermission>()
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new { rp.RoleId, rp.PermissionCode })
            .ToListAsync(cancellationToken);

        var permsByRoleId = permRows
            .GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PermissionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        return Ok(roles.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            name = r.Name,
            description = r.Description,
            permissions = permsByRoleId.TryGetValue(r.Id, out var perms) ? perms : Array.Empty<string>(),
            isSystem = r.IsSystemRole,
            isActive = true
        }));
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest data, CancellationToken cancellationToken = default)
    {
        var code = data.Code.Trim();
        var name = data.Name.Trim();
        if (string.IsNullOrWhiteSpace(code)) return BadRequest(new { message = "角色编码不能为空" });
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "角色名称不能为空" });

        var exists = await context.Roles.AnyAsync(r => r.Code == code, cancellationToken);
        if (exists) return BadRequest(new { message = "角色编码已存在" });

        var role = Role.Create(Guid.Empty, name, code, false);
        role.UpdateDetails(name, data.Description);
        if (data.Permissions is { Count: > 0 })
        {
            role.ReplacePermissions(data.Permissions);
        }

        await context.Roles.AddAsync(role, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { id = role.Id, message = "创建成功" });
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("roles/{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] JsonElement data, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role == null) return NotFound();

        if (data.ValueKind != JsonValueKind.Object) return BadRequest(new { message = "请求体格式不正确" });

        var name = role.Name;
        var description = role.Description;

        if (data.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
        {
            name = nameEl.GetString()!;
        }

        if (data.TryGetProperty("description", out var descEl))
        {
            if (descEl.ValueKind == JsonValueKind.Null) description = null;
            else if (descEl.ValueKind == JsonValueKind.String) description = descEl.GetString();
        }

        role.UpdateDetails(name, description);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 更新角色权限
    /// </summary>
    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest data, CancellationToken cancellationToken = default)
    {
        var roleExists = await context.Roles.AnyAsync(r => r.Id == id, cancellationToken);
        if (!roleExists) return NotFound();

        var desired = ((IEnumerable<string>?)data.Permissions ?? Array.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await context.Set<RolePermission>()
            .Where(rp => rp.RoleId == id)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(rp => !desired.Contains(rp.PermissionCode)).ToList();
        if (toRemove.Count > 0) context.Set<RolePermission>().RemoveRange(toRemove);

        var existingCodes = existing.Select(rp => rp.PermissionCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toAdd = desired.Except(existingCodes, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var code in toAdd)
        {
            await context.Set<RolePermission>().AddAsync(new RolePermission(id, code), cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "权限已更新" });
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role == null) return NotFound();
        context.Roles.Remove(role);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 获取所有权限
    /// </summary>
    [HttpGet("permissions")]
    public IActionResult GetPermissions()
    {
        var items = PermissionCatalog.All.Select(p => new { code = p.Code, name = p.Name, category = p.Category });
        return Ok(items);
    }

    /// <summary>
    /// 获取审批流程列表
    /// </summary>
    [HttpGet("approval-flows")]
    public IActionResult GetApprovalFlows()
    {
        return Ok(new[]
        {
            new { id = Guid.NewGuid(), name = "请假审批流程", type = "leave", steps = new[] { new { order = 1, approverType = "supervisor" }, new { order = 2, approverType = "hr" } }, isActive = true, description = "员工请假审批" },
            new { id = Guid.NewGuid(), name = "报销审批流程", type = "expense", steps = new[] { new { order = 1, approverType = "supervisor" }, new { order = 2, approverType = "department_head" } }, isActive = true, description = "费用报销审批" }
        });
    }

    /// <summary>
    /// 创建审批流程
    /// </summary>
    [HttpPost("approval-flows")]
    public IActionResult CreateApprovalFlow([FromBody] object data)
    {
        return Ok(new { id = Guid.NewGuid(), message = "创建成功" });
    }

    /// <summary>
    /// 更新审批流程
    /// </summary>
    [HttpPut("approval-flows/{id:guid}")]
    public IActionResult UpdateApprovalFlow(Guid id, [FromBody] object data)
    {
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除审批流程
    /// </summary>
    [HttpDelete("approval-flows/{id:guid}")]
    public IActionResult DeleteApprovalFlow(Guid id)
    {
        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 获取审计日志列表
    /// </summary>
    [HttpGet("audit-logs")]
    public IActionResult GetAuditLogs([FromQuery] string? action, [FromQuery] string? startDate, [FromQuery] string? endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(new
        {
            items = new[]
            {
                new { id = Guid.NewGuid(), userId = Guid.NewGuid(), userName = "admin", action = "login", entityType = "User", entityId = "", description = "用户登录系统", ipAddress = "192.168.1.100", createdAt = DateTime.Now.AddHours(-1) },
                new { id = Guid.NewGuid(), userId = Guid.NewGuid(), userName = "hr", action = "create", entityType = "Employee", entityId = Guid.NewGuid().ToString(), description = "创建员工档案", ipAddress = "192.168.1.101", createdAt = DateTime.Now.AddHours(-2) },
                new { id = Guid.NewGuid(), userId = Guid.NewGuid(), userName = "admin", action = "update", entityType = "Role", entityId = Guid.NewGuid().ToString(), description = "更新角色权限", ipAddress = "192.168.1.100", createdAt = DateTime.Now.AddHours(-3) }
            },
            total = 3,
            page,
            pageSize
        });
    }

    /// <summary>
    /// 导出审计日志
    /// </summary>
    [HttpGet("audit-logs/export")]
    public IActionResult ExportAuditLogs([FromQuery] string? action, [FromQuery] string? startDate, [FromQuery] string? endDate)
    {
        return Ok(new { message = "导出功能待实现" });
    }
}

public record CreateUserRequest(
    string Username,
    string? DisplayName,
    List<string> Roles,
    string? Email = null,
    string? Password = null,
    string? EmployeeId = null,
    string? Phone = null,
    bool? IsActive = null);

public record ResetPasswordRequest(string? NewPassword);

public record CreateRoleRequest(
    string Code,
    string Name,
    string? Description = null,
    List<string>? Permissions = null,
    bool? IsActive = null);

public record UpdateRolePermissionsRequest(List<string>? Permissions);

internal static class PermissionCatalog
{
    internal static readonly IReadOnlyList<(string Code, string Name, string Category)> All = Build();

    private static IReadOnlyList<(string Code, string Name, string Category)> Build()
    {
        var map = new Dictionary<string, (string Name, string Category)>(StringComparer.OrdinalIgnoreCase)
        {
            [Permissions.EmployeeRead] = ("员工查看", "员工"),
            [Permissions.EmployeeWrite] = ("员工编辑", "员工"),
            [Permissions.EmployeeDelete] = ("员工删除", "员工"),
            [Permissions.OrganizationRead] = ("组织查看", "组织"),
            [Permissions.OrganizationWrite] = ("组织编辑", "组织"),
            [Permissions.AttendanceRead] = ("考勤查看", "考勤"),
            [Permissions.AttendanceWrite] = ("考勤编辑", "考勤"),
            [Permissions.AttendanceApprove] = ("考勤审批", "考勤"),
            [Permissions.LeaveRead] = ("请假查看", "假期"),
            [Permissions.LeaveWrite] = ("请假申请", "假期"),
            [Permissions.LeaveApprove] = ("请假审批", "假期"),
            [Permissions.PayrollRead] = ("薪酬查看", "薪酬"),
            [Permissions.PayrollWrite] = ("薪酬编辑", "薪酬"),
            [Permissions.PayrollApprove] = ("薪酬审批", "薪酬"),
            [Permissions.ExpenseRead] = ("报销查看", "报销"),
            [Permissions.ExpenseWrite] = ("报销申请", "报销"),
            [Permissions.ExpenseApprove] = ("报销审批", "报销"),
            [Permissions.SystemAdmin] = ("系统管理员", "系统"),
            [Permissions.TenantAdmin] = ("租户管理员", "系统")
        };

        var codes = typeof(Permissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f is { IsLiteral: true, IsInitOnly: false })
            .Select(f => (string)f.GetRawConstantValue()!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return codes
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Select(code =>
            {
                if (map.TryGetValue(code, out var v)) return (code, v.Name, v.Category);
                var category = code.Contains(':', StringComparison.Ordinal) ? code.Split(':', 2)[0] : "其他";
                return (code, code, category);
            })
            .ToArray();
    }
}
