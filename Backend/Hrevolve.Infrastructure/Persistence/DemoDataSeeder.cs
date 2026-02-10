using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Hrevolve.Domain.Attendance;
using Hrevolve.Domain.Audit;
using Hrevolve.Domain.Employees;
using Hrevolve.Domain.Expense;
using Hrevolve.Domain.Identity;
using Hrevolve.Domain.Leave;
using Hrevolve.Domain.Organizations;
using Hrevolve.Domain.Payroll;
using Hrevolve.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hrevolve.Infrastructure.Persistence;

public class DemoDataSeeder(HrevolveDbContext context, ILogger<DemoDataSeeder> logger)
{
    public const string DemoTenantCode = "demo";
    public const string DemoAdminUsername = "demo_admin";
    public const string DemoAdminPassword = "demo123";

    private const int SeedRandom = 20260205;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await EnsureDemoTenantAsync(cancellationToken);
        await EnsureDemoRolesAndUsersAsync(tenant.Id, cancellationToken);

        var tenantId = tenant.Id;

        var random = new Random(SeedRandom);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var nowUtc = DateTime.UtcNow;

        var hasAnyBusinessData = await context.Employees.IgnoreQueryFilters()
            .AnyAsync(e => e.TenantId == tenantId, cancellationToken);
        if (hasAnyBusinessData)
        {
            logger.LogInformation("演示数据已存在，尝试补全演示考勤数据: {TenantCode}", DemoTenantCode);
            await EnsureAttendanceDemoDataAsync(tenantId, random, today, nowUtc, cancellationToken);
            await EnsureDemoUserEmployeeLinksAsync(tenantId, cancellationToken);
            await EnsureDemoUserLeaveAndPayrollAsync(tenantId, random, today, cancellationToken);
            return;
        }

        var org = CreateOrganizations(tenantId, random);
        await context.OrganizationUnits.AddRangeAsync(org.AllUnits, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var positions = CreatePositions(tenantId, org, random);
        await context.Positions.AddRangeAsync(positions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var employees = CreateEmployees(tenantId, org, positions, random, today);
        await context.Employees.AddRangeAsync(employees.AllEmployees, cancellationToken);
        await context.JobHistories.AddRangeAsync(employees.AllJobHistories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await LinkDemoUsersToEmployeesAsync(tenantId, employees, cancellationToken);

        var shifts = CreateShifts(tenantId);
        await context.Shifts.AddRangeAsync(shifts, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var schedulingWindow = new DateRange(today.AddDays(-90), today);
        var schedules = CreateSchedules(tenantId, employees.ScheduledEmployees, shifts, schedulingWindow, random);
        await context.Schedules.AddRangeAsync(schedules, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var attendanceRecords = CreateAttendanceRecords(tenantId, employees.ScheduledEmployees, schedules, shifts, random, nowUtc);
        await context.AttendanceRecords.AddRangeAsync(attendanceRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var leave = CreateLeave(tenantId, employees.AllEmployees, employees.DemoUsers.EmployeeId, random, today);
        await context.LeaveTypes.AddRangeAsync(leave.LeaveTypes, cancellationToken);
        await context.LeavePolicies.AddRangeAsync(leave.LeavePolicies, cancellationToken);
        await context.LeaveBalances.AddRangeAsync(leave.LeaveBalances, cancellationToken);
        await context.LeaveRequests.AddRangeAsync(leave.LeaveRequests, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var payroll = CreatePayroll(tenantId, employees.AllEmployees, random, today);
        await context.PayrollPeriods.AddRangeAsync(payroll.PayrollPeriods, cancellationToken);
        await context.PayrollItems.AddRangeAsync(payroll.PayrollItems, cancellationToken);
        await context.PayrollRecords.AddRangeAsync(payroll.PayrollRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var expenses = CreateExpenses(tenantId, employees.AllEmployees, payroll.RecentPeriodId, random, today);
        await context.ExpenseRequests.AddRangeAsync(expenses, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var auditLogs = CreateAuditLogs(tenantId, employees.DemoUsers.DemoAdminUserId, nowUtc);
        await context.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("演示数据创建完成: {TenantCode}", DemoTenantCode);
    }

    private async Task EnsureDemoUserEmployeeLinksAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var demoAdminUser = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == DemoAdminUsername, cancellationToken);
        var demoHrUser = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "demo_hr", cancellationToken);
        var demoEmployeeUser = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "demo_user", cancellationToken);

        if (demoAdminUser == null || demoHrUser == null || demoEmployeeUser == null) return;

        var ceoEmployeeId = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.EmployeeNumber == "CEO0001")
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var demoAdminEmployeeIdFallback = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.EmployeeNumber == "ADM1001")
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var demoHrEmployeeId = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.EmployeeNumber == "HR2001")
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var demoEmployeeId = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.EmployeeNumber == "EMP3001")
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var changed = false;

        var desiredAdminEmployeeId = ceoEmployeeId != Guid.Empty ? ceoEmployeeId : demoAdminEmployeeIdFallback;
        if (desiredAdminEmployeeId != Guid.Empty && demoAdminUser.EmployeeId != desiredAdminEmployeeId)
        {
            demoAdminUser.LinkEmployee(desiredAdminEmployeeId);
            changed = true;
        }

        if (demoHrEmployeeId != Guid.Empty && demoHrUser.EmployeeId != demoHrEmployeeId)
        {
            demoHrUser.LinkEmployee(demoHrEmployeeId);
            changed = true;
        }

        if (demoEmployeeId != Guid.Empty && demoEmployeeUser.EmployeeId != demoEmployeeId)
        {
            demoEmployeeUser.LinkEmployee(demoEmployeeId);
            changed = true;
        }

        if (!changed) return;

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "已修正演示账号与员工关联: demo_admin={AdminEmployeeNo}, demo_hr={HrEmployeeNo}, demo_user={EmployeeNo}",
            desiredAdminEmployeeId == ceoEmployeeId ? "CEO0001" : "ADM1001",
            "HR2001",
            "EMP3001");
    }

    private async Task EnsureDemoUserLeaveAndPayrollAsync(
        Guid tenantId,
        Random random,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var demoUsers = await context.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .Where(u => u.Username == DemoAdminUsername || u.Username == "demo_hr" || u.Username == "demo_user")
            .ToListAsync(cancellationToken);

        var employeeIds = demoUsers
            .Select(u => u.EmployeeId)
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0) return;

        await EnsureLeaveDemoDataForEmployeesAsync(tenantId, employeeIds, random, today, cancellationToken);
        await EnsurePayrollDemoDataForEmployeesAsync(tenantId, employeeIds, random, today, cancellationToken);
    }

    private async Task EnsureLeaveDemoDataForEmployeesAsync(
        Guid tenantId,
        List<Guid> employeeIds,
        Random random,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var requiredCodes = new[] { "ANNUAL", "SICK", "PERSONAL", "MATERNITY", "PATERNITY", "COMP" };

        var existingTypes = await context.LeaveTypes.IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var typeByCode = existingTypes
            .Where(t => !string.IsNullOrWhiteSpace(t.Code))
            .ToDictionary(t => t.Code, t => t, StringComparer.OrdinalIgnoreCase);

        var typesToAdd = new List<LeaveType>();
        if (!typeByCode.ContainsKey("ANNUAL")) typesToAdd.Add(CreateLeaveType(tenantId, "年假", "ANNUAL", true, "#52c41a", allowHalfDay: true));
        if (!typeByCode.ContainsKey("SICK")) typesToAdd.Add(CreateLeaveType(tenantId, "病假", "SICK", true, "#faad14", allowHalfDay: true, requiresAttachment: true));
        if (!typeByCode.ContainsKey("PERSONAL")) typesToAdd.Add(CreateLeaveType(tenantId, "事假", "PERSONAL", false, "#1890ff", allowHalfDay: true));
        if (!typeByCode.ContainsKey("MATERNITY")) typesToAdd.Add(CreateLeaveType(tenantId, "产假", "MATERNITY", true, "#eb2f96", allowHalfDay: false));
        if (!typeByCode.ContainsKey("PATERNITY")) typesToAdd.Add(CreateLeaveType(tenantId, "陪产假", "PATERNITY", true, "#722ed1", allowHalfDay: false));
        if (!typeByCode.ContainsKey("COMP")) typesToAdd.Add(CreateLeaveType(tenantId, "调休", "COMP", true, "#13c2c2", allowHalfDay: true));

        if (typesToAdd.Count > 0)
        {
            await context.LeaveTypes.AddRangeAsync(typesToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            foreach (var t in typesToAdd)
            {
                typeByCode[t.Code] = t;
            }
        }

        var types = requiredCodes.Select(code => typeByCode[code]).ToList();

        var existingPolicies = await context.LeavePolicies.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var policyByType = existingPolicies
            .GroupBy(p => p.LeaveTypeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.BaseQuota).First());

        var policiesToAdd = new List<LeavePolicy>();
        foreach (var t in types)
        {
            if (policyByType.ContainsKey(t.Id)) continue;
            var p = LeavePolicy.Create(tenantId, t.Id, $"{t.Name}规则", t.Code == "ANNUAL" ? 10m : 5m);
            SetPrivateProperty(p, "EffectiveAfterDays", t.Code == "ANNUAL" ? 90 : 0);
            SetPrivateProperty(p, "MaxCarryOverDays", t.Code == "ANNUAL" ? 5m : 0m);
            SetPrivateProperty(p, "CarryOverExpiryMonths", 6);
            SetPrivateProperty(p, "AccrualPeriod", t.Code == "ANNUAL" ? AccrualPeriod.Monthly : AccrualPeriod.Yearly);
            policiesToAdd.Add(p);
            policyByType[t.Id] = p;
        }

        if (policiesToAdd.Count > 0)
        {
            await context.LeavePolicies.AddRangeAsync(policiesToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var year = today.Year;
        var existingBalances = await context.LeaveBalances.IgnoreQueryFilters()
            .Where(b => b.TenantId == tenantId)
            .Where(b => b.Year == year)
            .Where(b => employeeIds.Contains(b.EmployeeId))
            .ToListAsync(cancellationToken);

        var balanceKey = existingBalances.ToDictionary(b => (b.EmployeeId, b.LeaveTypeId), b => b);

        var employeeHireDates = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.HireDate })
            .ToDictionaryAsync(x => x.Id, x => x.HireDate, cancellationToken);

        var balancesToAdd = new List<LeaveBalance>();
        foreach (var employeeId in employeeIds)
        {
            foreach (var t in types)
            {
                if (balanceKey.ContainsKey((employeeId, t.Id))) continue;
                var entitlement = t.Code == "ANNUAL" ? 10m : 5m;
                var b = LeaveBalance.Create(tenantId, employeeId, t.Id, year, entitlement);
                if (t.Code == "ANNUAL" && employeeHireDates.TryGetValue(employeeId, out var hire) && hire < today.AddYears(-2))
                {
                    b.SetCarryOver(2m);
                }
                balancesToAdd.Add(b);
                balanceKey[(employeeId, t.Id)] = b;
            }
        }

        if (balancesToAdd.Count > 0)
        {
            await context.LeaveBalances.AddRangeAsync(balancesToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var employeesWithRequests = await context.LeaveRequests.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Where(r => employeeIds.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var employeesMissingRequests = employeeIds.Except(employeesWithRequests).ToList();
        if (employeesMissingRequests.Count == 0) return;

        var annual = types.Single(t => t.Code == "ANNUAL");
        var sick = types.Single(t => t.Code == "SICK");

        var requestsToAdd = new List<LeaveRequest>();
        foreach (var employeeId in employeesMissingRequests)
        {
            var r1 = LeaveRequest.Create(tenantId, employeeId, annual.Id, today.AddDays(7), today.AddDays(8), DayPart.FullDay, DayPart.FullDay, "演示：家庭事务");
            balanceKey[(employeeId, annual.Id)].AddPending(r1.TotalDays);
            r1.Approve(Guid.Empty, "同意");
            balanceKey[(employeeId, annual.Id)].Use(r1.TotalDays);
            requestsToAdd.Add(r1);

            if (random.NextDouble() < 0.75)
            {
                var r2 = LeaveRequest.Create(tenantId, employeeId, sick.Id, today.AddDays(-10), today.AddDays(-10), DayPart.Morning, DayPart.Morning, "演示：就医复诊");
                balanceKey[(employeeId, sick.Id)].AddPending(r2.TotalDays);
                r2.Approve(Guid.Empty, "已核对病假证明");
                balanceKey[(employeeId, sick.Id)].Use(r2.TotalDays);
                SetAttachments(r2, ["/demo-assets/leave/doctor-note-001.txt"]);
                requestsToAdd.Add(r2);
            }
        }

        await context.LeaveRequests.AddRangeAsync(requestsToAdd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("已补全演示假期数据: Employees={EmployeeCount}, Requests+={RequestCount}", employeeIds.Count, requestsToAdd.Count);
    }

    private async Task EnsurePayrollDemoDataForEmployeesAsync(
        Guid tenantId,
        List<Guid> employeeIds,
        Random random,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-2);
        var desiredPeriods = Enumerable.Range(0, 3)
            .Select(i => start.AddMonths(i))
            .Select(m => new { m.Year, m.Month, Start = new DateOnly(m.Year, m.Month, 1), End = new DateOnly(m.Year, m.Month, 1).AddMonths(1).AddDays(-1) })
            .ToList();

        var existingPeriods = await context.PayrollPeriods.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .Where(p => desiredPeriods.Select(x => x.Year).Contains(p.Year))
            .ToListAsync(cancellationToken);

        var periodByYearMonth = existingPeriods.ToDictionary(p => (p.Year, p.Month), p => p);
        var periodsToAdd = new List<PayrollPeriod>();
        foreach (var p in desiredPeriods)
        {
            if (periodByYearMonth.ContainsKey((p.Year, p.Month))) continue;
            var payDate = p.End.AddDays(5);
            var created = PayrollPeriod.Create(tenantId, p.Year, p.Month, p.Start, p.End, payDate);
            periodsToAdd.Add(created);
            periodByYearMonth[(p.Year, p.Month)] = created;
        }

        if (periodsToAdd.Count > 0)
        {
            await context.PayrollPeriods.AddRangeAsync(periodsToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var codes = new[] { "BASE", "BONUS", "ALLOW_TRAVEL", "ALLOW_MEAL", "SI_EMP", "HF_EMP", "IIT" };
        var existingItems = await context.PayrollItems.IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var itemByCode = existingItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Code))
            .ToDictionary(i => i.Code, i => i, StringComparer.OrdinalIgnoreCase);

        var itemsToAdd = new List<PayrollItem>();
        if (!itemByCode.ContainsKey("BASE")) itemsToAdd.Add(PayrollItem.Create(tenantId, "基本工资", "BASE", PayrollItemType.Earning, CalculationType.Manual));
        if (!itemByCode.ContainsKey("BONUS")) itemsToAdd.Add(PayrollItem.Create(tenantId, "绩效奖金", "BONUS", PayrollItemType.Earning, CalculationType.Manual));
        if (!itemByCode.ContainsKey("ALLOW_TRAVEL")) itemsToAdd.Add(PayrollItem.Create(tenantId, "交通补贴", "ALLOW_TRAVEL", PayrollItemType.Earning, CalculationType.Fixed));
        if (!itemByCode.ContainsKey("ALLOW_MEAL")) itemsToAdd.Add(PayrollItem.Create(tenantId, "餐补", "ALLOW_MEAL", PayrollItemType.Earning, CalculationType.Fixed));
        if (!itemByCode.ContainsKey("SI_EMP")) itemsToAdd.Add(PayrollItem.Create(tenantId, "个人社保", "SI_EMP", PayrollItemType.Deduction, CalculationType.Manual));
        if (!itemByCode.ContainsKey("HF_EMP")) itemsToAdd.Add(PayrollItem.Create(tenantId, "个人公积金", "HF_EMP", PayrollItemType.Deduction, CalculationType.Manual));
        if (!itemByCode.ContainsKey("IIT")) itemsToAdd.Add(PayrollItem.Create(tenantId, "个税", "IIT", PayrollItemType.Tax, CalculationType.Manual));

        foreach (var item in itemsToAdd)
        {
            SetPrivateProperty(item, "IsActive", true);
            itemByCode[item.Code] = item;
        }

        if (itemsToAdd.Count > 0)
        {
            await context.PayrollItems.AddRangeAsync(itemsToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var travelItem = itemByCode["ALLOW_TRAVEL"];
        var mealItem = itemByCode["ALLOW_MEAL"];
        var iitItem = itemByCode["IIT"];
        var baseItem = itemByCode["BASE"];

        SetPrivateProperty(travelItem, "FixedAmount", 300m);
        SetPrivateProperty(mealItem, "FixedAmount", 600m);
        SetPrivateProperty(iitItem, "IsTaxable", false);
        SetPrivateProperty(baseItem, "IsTaxable", true);

        await context.SaveChangesAsync(cancellationToken);

        var periodIds = desiredPeriods.Select(p => periodByYearMonth[(p.Year, p.Month)].Id).ToList();
        var existingRecords = await context.PayrollRecords.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Where(r => employeeIds.Contains(r.EmployeeId))
            .Where(r => periodIds.Contains(r.PayrollPeriodId))
            .ToListAsync(cancellationToken);
        var recordKey = existingRecords.Select(r => (r.EmployeeId, r.PayrollPeriodId)).ToHashSet();

        var baseSalaryByEmployee = await context.JobHistories.IgnoreQueryFilters()
            .Where(j => j.TenantId == tenantId)
            .Where(j => employeeIds.Contains(j.EmployeeId))
            .Where(j => j.EffectiveEndDate == new DateOnly(9999, 12, 31) && j.CorrectionStatus == null)
            .GroupBy(j => j.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, BaseSalary = g.OrderByDescending(x => x.EffectiveStartDate).First().BaseSalary })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.BaseSalary, cancellationToken);

        var recordsToAdd = new List<PayrollRecord>();
        foreach (var employeeId in employeeIds)
        {
            var baseSalary = baseSalaryByEmployee.TryGetValue(employeeId, out var s) ? s : random.Next(20000, 80000);
            foreach (var p in desiredPeriods)
            {
                var periodId = periodByYearMonth[(p.Year, p.Month)].Id;
                if (recordKey.Contains((employeeId, periodId))) continue;

                var record = PayrollRecord.Create(tenantId, employeeId, periodId, baseSalary);
                record.AddDetail(baseItem.Id, "基本工资", PayrollItemType.Earning, baseSalary);
                if (random.NextDouble() < 0.55)
                {
                    record.AddDetail(itemByCode["BONUS"].Id, "绩效奖金", PayrollItemType.Earning, random.Next(500, 8000));
                }
                record.AddDetail(travelItem.Id, "交通补贴", PayrollItemType.Earning, 300);
                record.AddDetail(mealItem.Id, "餐补", PayrollItemType.Earning, 600);

                var si = Math.Round(baseSalary * 0.10m, 2);
                var hf = Math.Round(baseSalary * 0.07m, 2);
                var tax = Math.Round(Math.Max(0, (baseSalary - 5000m) * 0.03m), 2);

                record.AddDetail(itemByCode["SI_EMP"].Id, "个人社保", PayrollItemType.Deduction, si);
                record.AddDetail(itemByCode["HF_EMP"].Id, "个人公积金", PayrollItemType.Deduction, hf);
                record.AddDetail(iitItem.Id, "个税", PayrollItemType.Tax, tax);

                record.Calculate();
                record.Approve();
                if (random.NextDouble() < 0.65)
                {
                    record.MarkAsPaid();
                }

                recordsToAdd.Add(record);
                recordKey.Add((employeeId, periodId));
            }
        }

        if (recordsToAdd.Count == 0) return;

        await context.PayrollRecords.AddRangeAsync(recordsToAdd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("已补全演示薪资数据: Employees={EmployeeCount}, Records+={RecordCount}", employeeIds.Count, recordsToAdd.Count);
    }

    private async Task EnsureAttendanceDemoDataAsync(
        Guid tenantId,
        Random random,
        DateOnly today,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var employees = await context.Employees.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId)
            .OrderBy(e => e.EmployeeNumber)
            .ToListAsync(cancellationToken);
        if (employees.Count == 0) return;

        var scheduledEmployees = employees.Take(Math.Min(36, employees.Count)).ToList();
        EnsureScheduledEmployeeNumber(employees, scheduledEmployees, "EMP3001");
        EnsureScheduledEmployeeNumber(employees, scheduledEmployees, "ADM1001");
        EnsureScheduledEmployeeNumber(employees, scheduledEmployees, "HR2001");

        var existingShifts = await context.Shifts.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var shiftByCode = existingShifts.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var desired in CreateShifts(tenantId))
        {
            if (!shiftByCode.ContainsKey(desired.Code))
            {
                await context.Shifts.AddAsync(desired, cancellationToken);
                shiftByCode[desired.Code] = desired;
            }
        }
        await context.SaveChangesAsync(cancellationToken);

        var shifts = shiftByCode.Values.ToList();
        var dayShift = shifts.FirstOrDefault(s => string.Equals(s.Code, "DAY", StringComparison.OrdinalIgnoreCase)) ?? shifts.First();
        var flexShift = shifts.FirstOrDefault(s => string.Equals(s.Code, "FLEX", StringComparison.OrdinalIgnoreCase)) ?? dayShift;
        var nightShift = shifts.FirstOrDefault(s => string.Equals(s.Code, "NIGHT", StringComparison.OrdinalIgnoreCase)) ?? dayShift;

        var schedulingWindow = new DateRange(today.AddDays(-90), today);

        var employeeIds = scheduledEmployees.Select(e => e.Id).ToList();
        var existingSchedules = await context.Schedules.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .Where(s => employeeIds.Contains(s.EmployeeId))
            .Where(s => s.ScheduleDate >= schedulingWindow.Start && s.ScheduleDate <= schedulingWindow.End)
            .ToListAsync(cancellationToken);
        var scheduleByKey = existingSchedules.ToDictionary(s => (s.EmployeeId, s.ScheduleDate), s => s);

        var schedulesToAdd = new List<Schedule>();
        foreach (var employee in scheduledEmployees)
        {
            for (var date = schedulingWindow.Start; date <= schedulingWindow.End; date = date.AddDays(1))
            {
                if (scheduleByKey.ContainsKey((employee.Id, date))) continue;

                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                if (isWeekend)
                {
                    var rest = Schedule.Create(tenantId, employee.Id, dayShift.Id, date, true);
                    scheduleByKey[(employee.Id, date)] = rest;
                    schedulesToAdd.Add(rest);
                    continue;
                }

                var dice = CreateStableRandom(employee.Id, date, 11).NextDouble();
                var shiftId = dice switch
                {
                    < 0.75 => dayShift.Id,
                    < 0.95 => flexShift.Id,
                    _ => nightShift.Id
                };

                var sched = Schedule.Create(tenantId, employee.Id, shiftId, date, false);
                scheduleByKey[(employee.Id, date)] = sched;
                schedulesToAdd.Add(sched);
            }
        }

        if (schedulesToAdd.Count > 0)
        {
            await context.Schedules.AddRangeAsync(schedulesToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var existingRecords = await context.AttendanceRecords.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Where(r => employeeIds.Contains(r.EmployeeId))
            .Where(r => r.AttendanceDate >= schedulingWindow.Start && r.AttendanceDate <= schedulingWindow.End)
            .ToListAsync(cancellationToken);
        var recordByKey = existingRecords.ToDictionary(r => (r.EmployeeId, r.AttendanceDate), r => r);

        var shiftById = shifts.ToDictionary(s => s.Id, s => s);
        var recordsToAdd = new List<AttendanceRecord>();

        foreach (var schedule in scheduleByKey.Values.Where(s => !s.IsRestDay))
        {
            if (recordByKey.ContainsKey((schedule.EmployeeId, schedule.ScheduleDate))) continue;

            var r = CreateDemoAttendanceRecord(tenantId, schedule, shiftById, nowUtc);
            recordByKey[(schedule.EmployeeId, schedule.ScheduleDate)] = r;
            recordsToAdd.Add(r);
        }

        if (recordsToAdd.Count > 0)
        {
            await context.AttendanceRecords.AddRangeAsync(recordsToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("演示考勤数据补全完成: Shifts={Shifts}, Schedules+={SchedulesAdded}, Records+={RecordsAdded}", shifts.Count, schedulesToAdd.Count, recordsToAdd.Count);
    }

    private static void EnsureScheduledEmployeeNumber(List<Employee> allEmployees, List<Employee> scheduledEmployees, string employeeNumber)
    {
        var employee = allEmployees.FirstOrDefault(e => e.EmployeeNumber == employeeNumber);
        if (employee == null) return;
        if (scheduledEmployees.Any(e => e.Id == employee.Id)) return;

        if (scheduledEmployees.Count == 0)
        {
            scheduledEmployees.Add(employee);
            return;
        }

        var reservedIds = new HashSet<Guid>(
            scheduledEmployees
                .Where(e => e.EmployeeNumber is "EMP3001" or "ADM1001" or "HR2001")
                .Select(e => e.Id));

        var replaceIndex = scheduledEmployees.FindLastIndex(e => !reservedIds.Contains(e.Id));
        if (replaceIndex < 0) replaceIndex = scheduledEmployees.Count - 1;
        scheduledEmployees[replaceIndex] = employee;
    }

    private static AttendanceRecord CreateDemoAttendanceRecord(
        Guid tenantId,
        Schedule schedule,
        Dictionary<Guid, Shift> shiftById,
        DateTime nowUtc)
    {
        var random = CreateStableRandom(schedule.EmployeeId, schedule.ScheduleDate, 21);
        var record = AttendanceRecord.Create(tenantId, schedule.EmployeeId, schedule.ScheduleDate, schedule.Id);

        var dice = random.NextDouble();
        if (dice < 0.08)
        {
            return record;
        }

        shiftById.TryGetValue(schedule.ShiftId, out var shift);
        var methodRoll = random.NextDouble();
        var method = methodRoll switch
        {
            < 0.7 => CheckInMethod.App,
            < 0.9 => CheckInMethod.WiFi,
            < 0.97 => CheckInMethod.Device,
            _ => CheckInMethod.Web
        };

        var localDate = DateTime.SpecifyKind(schedule.ScheduleDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), DateTimeKind.Local);
        var workStart = localDate;
        if (shift != null)
        {
            workStart = localDate.AddHours(shift.StartTime.Hour).AddMinutes(shift.StartTime.Minute);
        }

        var checkInDelayMinutes = (int)Math.Round(ClampNormal(random, 3, 12, -10, 90));
        var checkIn = workStart.AddMinutes(checkInDelayMinutes);
        record.CheckIn(checkIn.ToUniversalTime(), method, "31.2304,121.4737");

        if (dice < 0.18)
        {
            if (random.NextDouble() < 0.45)
            {
                record.ManualCheckOut(checkIn.AddHours(8).AddMinutes(random.Next(0, 40)).ToUniversalTime(), "补卡：系统异常导致签退缺失");
                record.Approve(Guid.Empty);
                SetPrivateProperty(record, "ApprovedAt", nowUtc);
            }
            return record;
        }

        var workEnd = localDate.AddHours(18);
        var crossDay = false;
        if (shift != null)
        {
            workEnd = localDate.AddHours(shift.EndTime.Hour).AddMinutes(shift.EndTime.Minute);
            crossDay = shift.CrossDay || shift.EndTime < shift.StartTime;
        }
        if (crossDay) workEnd = workEnd.AddDays(1);

        var checkoutDeltaMinutes = (int)Math.Round(ClampNormal(random, -2, 20, -120, 180));
        var checkOut = workEnd.AddMinutes(checkoutDeltaMinutes);
        record.CheckOut(checkOut.ToUniversalTime(), method, "31.2304,121.4737");

        if (checkOut > checkIn)
        {
            SetPrivateProperty(record, "OvertimeHours", (decimal)Math.Max(0, (checkOut - workEnd).TotalHours));
        }

        if (random.NextDouble() < 0.12)
        {
            SetPrivateProperty(record, "Remarks", $"演示：外出/会议 {random.Next(1, 50)}");
        }

        if (random.NextDouble() < 0.2)
        {
            record.Approve(Guid.Empty);
            SetPrivateProperty(record, "ApprovedAt", nowUtc);
        }

        return record;
    }

    private static Random CreateStableRandom(Guid employeeId, DateOnly date, int salt)
    {
        unchecked
        {
            var seed = SeedRandom;
            seed = (seed * 397) ^ employeeId.GetHashCode();
            seed = (seed * 397) ^ date.GetHashCode();
            seed = (seed * 397) ^ salt;
            return new Random(seed);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == DemoTenantCode, cancellationToken);
        if (tenant == null)
        {
            logger.LogInformation("未找到演示租户，跳过回滚: {TenantCode}", DemoTenantCode);
            return;
        }

        var tenantId = tenant.Id;

        await context.Set<RolePermission>().IgnoreQueryFilters().Where(p => context.Roles.IgnoreQueryFilters()
                .Where(r => r.TenantId == tenantId)
                .Select(r => r.Id)
                .Contains(p.RoleId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.UserRoles.IgnoreQueryFilters()
            .Where(ur => context.Users.IgnoreQueryFilters().Where(u => u.TenantId == tenantId).Select(u => u.Id).Contains(ur.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<TrustedDevice>().Where(td => context.Users.IgnoreQueryFilters().Where(u => u.TenantId == tenantId).Select(u => u.Id).Contains(td.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.ExternalLogins.Where(el => context.Users.IgnoreQueryFilters().Where(u => u.TenantId == tenantId).Select(u => u.Id).Contains(el.UserId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.AttendanceRecords.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.Schedules.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.Shifts.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Set<LeaveApproval>().Where(a => context.LeaveRequests.IgnoreQueryFilters().Where(l => l.TenantId == tenantId).Select(l => l.Id).Contains(a.LeaveRequestId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.LeaveRequests.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.LeaveBalances.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.LeavePolicies.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.LeaveTypes.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Set<PayrollDetail>().Where(d => context.PayrollRecords.IgnoreQueryFilters().Where(pr => pr.TenantId == tenantId).Select(pr => pr.Id).Contains(d.PayrollRecordId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.PayrollRecords.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.PayrollItems.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.PayrollPeriods.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Set<ExpenseApproval>().Where(a => context.ExpenseRequests.IgnoreQueryFilters().Where(er => er.TenantId == tenantId).Select(er => er.Id).Contains(a.ExpenseRequestId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Set<ExpenseItem>().Where(i => context.ExpenseRequests.IgnoreQueryFilters().Where(er => er.TenantId == tenantId).Select(er => er.Id).Contains(i.ExpenseRequestId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.ExpenseRequests.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.JobHistories.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.Employees.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Positions.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.OrganizationUnits.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Users.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await context.Roles.IgnoreQueryFilters().Where(r => r.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.AuditLogs.Where(a => a.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        await context.Tenants.Where(t => t.Id == tenantId).ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("演示数据回滚完成: {TenantCode}", DemoTenantCode);
    }

    public static IReadOnlyList<ExpenseTypeDefinition> GetExpenseTypeDefinitions() =>
    [
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000101"), "TRAVEL", "差旅费", ExpenseCategory.Travel, true, true, 5000m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000102"), "MEAL", "餐饮费", ExpenseCategory.Meals, true, true, 500m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000103"), "TRANSPORT", "交通费", ExpenseCategory.Transportation, true, false, 1000m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000104"), "ACCOM", "住宿费", ExpenseCategory.Accommodation, true, true, 3000m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000105"), "OFFICE", "办公用品", ExpenseCategory.Office, true, false, 2000m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000106"), "TRAIN", "培训费", ExpenseCategory.Training, true, true, 8000m),
        new ExpenseTypeDefinition(Guid.Parse("00000000-0000-0000-0000-000000000107"), "OTHER", "其他", ExpenseCategory.Other, false, false, null)
    ];

    public static ExpenseTypeDefinition? TryMapExpenseType(Guid typeId) =>
        GetExpenseTypeDefinitions().FirstOrDefault(x => x.Id == typeId);

    private async Task<Tenant> EnsureDemoTenantAsync(CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == DemoTenantCode, cancellationToken);
        if (tenant != null) return tenant;

        tenant = Tenant.Create("Hrevolve 演示租户", DemoTenantCode, TenantPlan.Enterprise);
        await context.Tenants.AddAsync(tenant, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    private async Task EnsureDemoRolesAndUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == "system_admin", cancellationToken);

        if (role == null)
        {
            role = Role.Create(tenantId, "系统管理员", "system_admin", true);
            role.AddPermission(Permissions.SystemAdmin);
            await context.Roles.AddAsync(role, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var user = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == DemoAdminUsername, cancellationToken);

        if (user == null)
        {
            user = User.Create(tenantId, DemoAdminUsername, "demo_admin@hrevolve.com");
            user.SetPassword(DemoAdminPassword);
            user.AddRole(role.Id);
            await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var hrRole = await context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == "hr_admin", cancellationToken);

        if (hrRole == null)
        {
            hrRole = Role.Create(tenantId, "HR管理员", "hr_admin", true);
            hrRole.AddPermission(Permissions.EmployeeRead);
            hrRole.AddPermission(Permissions.EmployeeWrite);
            hrRole.AddPermission(Permissions.OrganizationRead);
            hrRole.AddPermission(Permissions.AttendanceRead);
            hrRole.AddPermission(Permissions.LeaveRead);
            hrRole.AddPermission(Permissions.LeaveApprove);
            hrRole.AddPermission(Permissions.PayrollRead);
            hrRole.AddPermission(Permissions.ExpenseRead);
            hrRole.AddPermission(Permissions.ExpenseApprove);
            await context.Roles.AddAsync(hrRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var hrUser = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "demo_hr", cancellationToken);

        if (hrUser == null)
        {
            hrUser = User.Create(tenantId, "demo_hr", "demo_hr@hrevolve.com");
            hrUser.SetPassword("demo123");
            hrUser.AddRole(hrRole.Id);
            await context.Users.AddAsync(hrUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var employeeRole = await context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == "employee", cancellationToken);

        if (employeeRole == null)
        {
            employeeRole = Role.Create(tenantId, "普通员工", "employee", true);
            employeeRole.AddPermission(Permissions.AttendanceRead);
            employeeRole.AddPermission(Permissions.LeaveRead);
            employeeRole.AddPermission(Permissions.LeaveWrite);
            employeeRole.AddPermission(Permissions.ExpenseRead);
            employeeRole.AddPermission(Permissions.ExpenseWrite);
            employeeRole.AddPermission(Permissions.PayrollRead);
            await context.Roles.AddAsync(employeeRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var employeeUser = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "demo_user", cancellationToken);

        if (employeeUser == null)
        {
            employeeUser = User.Create(tenantId, "demo_user", "demo_user@hrevolve.com");
            employeeUser.SetPassword("demo123");
            employeeUser.AddRole(employeeRole.Id);
            await context.Users.AddAsync(employeeUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task LinkDemoUsersToEmployeesAsync(Guid tenantId, EmployeeSeedResult employees, CancellationToken cancellationToken)
    {
        var demoAdminUser = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.TenantId == tenantId && u.Username == DemoAdminUsername, cancellationToken);
        var demoHrUser = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.TenantId == tenantId && u.Username == "demo_hr", cancellationToken);
        var demoEmployeeUser = await context.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.TenantId == tenantId && u.Username == "demo_user", cancellationToken);

        demoAdminUser.LinkEmployee(employees.DemoUsers.EmployeeId);
        demoHrUser.LinkEmployee(employees.DemoUsers.HrEmployeeId);
        demoEmployeeUser.LinkEmployee(employees.DemoUsers.RegularEmployeeId);

        await context.SaveChangesAsync(cancellationToken);

        employees.DemoUsers.DemoAdminUserId = demoAdminUser.Id;
        employees.DemoUsers.DemoHrUserId = demoHrUser.Id;
        employees.DemoUsers.DemoUserId = demoEmployeeUser.Id;
    }

    private static OrganizationSeedResult CreateOrganizations(Guid tenantId, Random random)
    {
        var company = OrganizationUnit.Create(tenantId, "Hrevolve 演示公司", "COMP", OrganizationUnitType.Company);

        var rnd = new Func<int>(() => random.Next(1, 999));

        var divisions =
            new[]
            {
                OrganizationUnit.Create(tenantId, "技术中心", $"DIV-TECH-{rnd():000}", OrganizationUnitType.Division, company),
                OrganizationUnit.Create(tenantId, "运营中心", $"DIV-OPS-{rnd():000}", OrganizationUnitType.Division, company),
                OrganizationUnit.Create(tenantId, "共享服务中心", $"DIV-SSC-{rnd():000}", OrganizationUnitType.Division, company)
            };

        var departments = new[]
        {
            OrganizationUnit.Create(tenantId, "平台研发部", "DEPT-PLAT", OrganizationUnitType.Department, divisions[0]),
            OrganizationUnit.Create(tenantId, "产品设计部", "DEPT-PROD", OrganizationUnitType.Department, divisions[0]),
            OrganizationUnit.Create(tenantId, "数据与AI部", "DEPT-DATA", OrganizationUnitType.Department, divisions[0]),
            OrganizationUnit.Create(tenantId, "市场部", "DEPT-MKT", OrganizationUnitType.Department, divisions[1]),
            OrganizationUnit.Create(tenantId, "客户成功部", "DEPT-CS", OrganizationUnitType.Department, divisions[1]),
            OrganizationUnit.Create(tenantId, "HR部", "DEPT-HR", OrganizationUnitType.Department, divisions[2]),
            OrganizationUnit.Create(tenantId, "财务部", "DEPT-FIN", OrganizationUnitType.Department, divisions[2])
        };

        var all = new List<OrganizationUnit> { company };
        all.AddRange(divisions);
        all.AddRange(departments);

        return new OrganizationSeedResult(company, divisions, departments, all);
    }

    private static List<Position> CreatePositions(Guid tenantId, OrganizationSeedResult org, Random random)
    {
        var positions = new List<Position>();

        void add(Guid departmentId, string code, string name, PositionLevel level, decimal min, decimal max, string? sequence)
        {
            var p = Position.Create(tenantId, name, code, departmentId, level);
            p.SetSalaryRange(min, max);
            SetPrivateProperty(p, "Sequence", sequence);
            positions.Add(p);
        }

        add(org.Departments.Single(d => d.Code == "DEPT-PLAT").Id, "ENG-JR", "后端工程师", PositionLevel.Junior, 12000, 22000, "技术");
        add(org.Departments.Single(d => d.Code == "DEPT-PLAT").Id, "ENG-SR", "高级后端工程师", PositionLevel.Senior, 22000, 38000, "技术");
        add(org.Departments.Single(d => d.Code == "DEPT-PLAT").Id, "ENG-LD", "技术主管", PositionLevel.Lead, 32000, 52000, "技术");
        add(org.Departments.Single(d => d.Code == "DEPT-PROD").Id, "PM", "产品经理", PositionLevel.Senior, 20000, 40000, "产品");
        add(org.Departments.Single(d => d.Code == "DEPT-PROD").Id, "UX", "UX设计师", PositionLevel.Junior, 12000, 25000, "设计");
        add(org.Departments.Single(d => d.Code == "DEPT-DATA").Id, "DS", "数据科学家", PositionLevel.Senior, 26000, 52000, "数据");
        add(org.Departments.Single(d => d.Code == "DEPT-MKT").Id, "MKT", "市场专员", PositionLevel.Entry, 8000, 15000, "市场");
        add(org.Departments.Single(d => d.Code == "DEPT-CS").Id, "CS", "客户成功", PositionLevel.Junior, 10000, 18000, "运营");
        add(org.Departments.Single(d => d.Code == "DEPT-HR").Id, "HR", "HR专员", PositionLevel.Junior, 9000, 16000, "职能");
        add(org.Departments.Single(d => d.Code == "DEPT-FIN").Id, "FIN", "财务会计", PositionLevel.Junior, 10000, 20000, "职能");

        add(org.Company.Id, "CEO", "总经理", PositionLevel.CLevel, 60000, 120000, "管理");
        add(org.Company.Id, "VP-ENG", "技术VP", PositionLevel.VP, 70000, 110000, "管理");
        add(org.Company.Id, "HRD", "HR总监", PositionLevel.Director, 40000, 80000, "管理");

        var inactive = Position.Create(tenantId, "已停用示例职位", "INACTIVE", org.Company.Id, PositionLevel.Entry);
        inactive.SetSalaryRange(5000, 6000);
        inactive.Deactivate();
        positions.Add(inactive);

        return positions;
    }

    private static EmployeeSeedResult CreateEmployees(Guid tenantId, OrganizationSeedResult org, List<Position> positions, Random random, DateOnly today)
    {
        var employees = new List<Employee>();
        var histories = new List<JobHistory>();

        var deptById = org.Departments.ToDictionary(d => d.Id, d => d);
        var positionsByDept = positions.Where(p => deptById.ContainsKey(p.OrganizationUnitId)).GroupBy(p => p.OrganizationUnitId).ToDictionary(g => g.Key, g => g.ToList());

        var special = CreateDemoUsersEmployees(tenantId, org, positions);
        employees.AddRange([special.AdminEmployee, special.HrEmployee, special.RegularEmployee]);

        histories.AddRange([
            JobHistory.Create(tenantId, special.AdminEmployee.Id, special.AdminPositionId, special.AdminDepartmentId, 90000m, today.AddYears(-2), JobChangeType.Promotion, "演示管理员"),
            JobHistory.Create(tenantId, special.HrEmployee.Id, special.HrPositionId, special.HrDepartmentId, 22000m, today.AddYears(-1), JobChangeType.NewHire, "HR管理员"),
            JobHistory.Create(tenantId, special.RegularEmployee.Id, special.RegularPositionId, special.RegularDepartmentId, 16000m, today.AddMonths(-8), JobChangeType.NewHire, "演示员工")
        ]);

        var lastNames = new[] { "赵", "钱", "孙", "李", "周", "吴", "郑", "王", "冯", "陈", "褚", "卫", "蒋", "沈", "韩", "杨" };
        var firstNames = new[] { "一", "二", "三", "四", "五", "六", "小明", "小红", "子涵", "思远", "若曦", "宇航", "嘉怡", "晨曦", "梓轩" };
        var englishNames = new[] { "Alex", "Bella", "Chris", "Daisy", "Ethan", "Fiona", "Grace", "Henry", "Iris", "Jason", "Kevin", "Luna", "Mia", "Noah", "Olivia" };

        DateOnly randomBirth()
        {
            var year = random.Next(today.Year - 45, today.Year - 20);
            var month = random.Next(1, 13);
            var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
            return new DateOnly(year, month, day);
        }

        DateOnly randomHire()
        {
            var start = today.AddYears(-5).DayNumber;
            var end = today.AddDays(-5).DayNumber;
            return DateOnly.FromDayNumber(random.Next(start, end));
        }

        for (var i = 1; i <= 120; i++)
        {
            var employeeNo = $"E{(i + 1000).ToString(CultureInfo.InvariantCulture)}";
            var ln = lastNames[random.Next(lastNames.Length)];
            var fn = firstNames[random.Next(firstNames.Length)];
            var gender = (Gender)random.Next(0, 3);
            var dob = randomBirth();
            var hire = randomHire();
            var empType = (EmploymentType)random.Next(0, 5);

            var employee = Employee.Create(tenantId, employeeNo, fn, ln, gender, dob, hire, empType);

            if (i % 8 == 0)
            {
                employee.SetContactInfo(null, null, null, i % 16 == 0 ? "地址缺失(边界用例)" : null);
            }
            else
            {
                var email = $"user{employeeNo.ToLowerInvariant()}@demo.hrevolve.com";
                var phone = $"13{random.Next(0, 10)}{random.Next(100000000, 999999999)}";
                var personalEmail = i % 10 == 0 ? null : $"p.{email}";
                employee.SetContactInfo(email, phone, personalEmail, $"上海市浦东新区世纪大道{random.Next(1, 999)}号");
            }

            if (i % 9 == 0)
            {
                SetPrivateProperty(employee, "EnglishName", englishNames[random.Next(englishNames.Length)]);
            }

            if (i % 20 == 0)
            {
                SetPrivateProperty(employee, "Status", EmploymentStatus.OnLeave);
            }

            if (i % 33 == 0)
            {
                var termination = hire.AddYears(1).AddDays(random.Next(5, 200));
                employee.Terminate(termination);
            }

            employees.Add(employee);

            var dept = org.Departments[random.Next(org.Departments.Length)];
            var posCandidates = positionsByDept[dept.Id];
            var pos = posCandidates[random.Next(posCandidates.Count)];
            var baseSalary = random.Next(9000, 45000);
            var jh = JobHistory.Create(tenantId, employee.Id, pos.Id, dept.Id, baseSalary, hire, JobChangeType.NewHire, "入职");
            histories.Add(jh);

            if (i % 12 == 0)
            {
                jh.Close(hire.AddMonths(6));
                var newPos = posCandidates[random.Next(posCandidates.Count)];
                var promotion = JobHistory.Create(tenantId, employee.Id, newPos.Id, dept.Id, baseSalary + random.Next(1000, 8000), hire.AddMonths(6), JobChangeType.Promotion, "晋升/调薪");
                histories.Add(promotion);
            }
        }

        var managerCandidates = employees.Where(e => e.EmployeeNumber is not null).Take(12).ToArray();
        foreach (var e in employees.Where((_, idx) => idx % 5 == 0))
        {
            var manager = managerCandidates[random.Next(managerCandidates.Length)];
            if (manager.Id != e.Id)
            {
                e.SetDirectManager(manager.Id);
            }
        }

        var scheduled = employees.Where((_, idx) => idx % 3 == 0).Take(36).ToList();
        EnsureScheduledEmployee(scheduled, special.RegularEmployee);
        EnsureScheduledEmployee(scheduled, special.AdminEmployee);
        EnsureScheduledEmployee(scheduled, special.HrEmployee);

        if (!scheduled.Any(e => e.Id == special.RegularEmployee.Id))
        {
            scheduled[0] = special.RegularEmployee;
        }

        return new EmployeeSeedResult(employees, histories, scheduled, special.Users);
    }

    private static void EnsureScheduledEmployee(List<Employee> scheduled, Employee employee)
    {
        if (scheduled.Any(e => e.Id == employee.Id)) return;
        if (scheduled.Count == 0)
        {
            scheduled.Add(employee);
            return;
        }

        var existingIds = scheduled.Select(e => e.Id).ToHashSet();
        if (existingIds.Contains(employee.Id)) return;

        var replaceIndex = scheduled.FindLastIndex(e => e.Id != employee.Id);
        if (replaceIndex < 0) replaceIndex = scheduled.Count - 1;
        scheduled[replaceIndex] = employee;
    }

    private static DemoUsersEmployees CreateDemoUsersEmployees(Guid tenantId, OrganizationSeedResult org, List<Position> positions)
    {
        var adminEmployee = Employee.Create(tenantId, "ADM1001", "宇航", "陈", Gender.Male, new DateOnly(1990, 6, 18), DateOnly.FromDateTime(DateTime.Today).AddYears(-2), EmploymentType.FullTime);
        adminEmployee.SetContactInfo("demo_admin@hrevolve.com", "13800000001", null, "上海市黄浦区演示路1号");

        var hrEmployee = Employee.Create(tenantId, "HR2001", "若曦", "周", Gender.Female, new DateOnly(1992, 3, 8), DateOnly.FromDateTime(DateTime.Today).AddYears(-1), EmploymentType.FullTime);
        hrEmployee.SetContactInfo("demo_hr@hrevolve.com", "13800000002", null, "上海市黄浦区演示路2号");

        var regularEmployee = Employee.Create(tenantId, "EMP3001", "小明", "李", Gender.Male, new DateOnly(1998, 11, 2), DateOnly.FromDateTime(DateTime.Today).AddMonths(-8), EmploymentType.FullTime);
        regularEmployee.SetContactInfo("demo_user@hrevolve.com", "13800000003", null, "上海市黄浦区演示路3号");

        var adminDept = org.Departments.Single(d => d.Code == "DEPT-PLAT");
        var hrDept = org.Departments.Single(d => d.Code == "DEPT-HR");
        var regularDept = org.Departments.Single(d => d.Code == "DEPT-PLAT");

        var adminPos = positions.Single(p => p.Code == "VP-ENG");
        var hrPos = positions.Single(p => p.Code == "HR");
        var regularPos = positions.Single(p => p.Code == "ENG-JR");

        var demo = new DemoUsersIds(adminEmployee.Id, hrEmployee.Id, regularEmployee.Id);
        return new DemoUsersEmployees(adminEmployee, hrEmployee, regularEmployee, adminDept.Id, hrDept.Id, regularDept.Id, adminPos.Id, hrPos.Id, regularPos.Id, demo);
    }

    private static List<Shift> CreateShifts(Guid tenantId)
    {
        var day = Shift.Create(tenantId, "标准班次", "DAY", new TimeOnly(9, 0), new TimeOnly(18, 0), 8m);
        day.SetBreakTime(new TimeOnly(12, 0), new TimeOnly(13, 0));
        day.SetFlexibleTime(15, 15);

        var flex = Shift.Create(tenantId, "弹性班次", "FLEX", new TimeOnly(10, 0), new TimeOnly(19, 0), 8m);
        flex.SetBreakTime(new TimeOnly(12, 30), new TimeOnly(13, 30));
        flex.SetFlexibleTime(60, 60);

        var night = Shift.Create(tenantId, "夜班", "NIGHT", new TimeOnly(22, 0), new TimeOnly(6, 0), 7m);
        night.SetBreakTime(new TimeOnly(1, 0), new TimeOnly(1, 30));
        night.SetFlexibleTime(10, 10);

        return [day, flex, night];
    }

    private static List<Schedule> CreateSchedules(Guid tenantId, List<Employee> scheduledEmployees, List<Shift> shifts, DateRange range, Random random)
    {
        var schedule = new List<Schedule>();
        var dayShift = shifts.Single(s => s.Code == "DAY");
        var flex = shifts.Single(s => s.Code == "FLEX");
        var night = shifts.Single(s => s.Code == "NIGHT");

        foreach (var employee in scheduledEmployees)
        {
            for (var date = range.Start; date <= range.End; date = date.AddDays(1))
            {
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                if (isWeekend)
                {
                    schedule.Add(Schedule.Create(tenantId, employee.Id, dayShift.Id, date, true));
                    continue;
                }

                var dice = random.NextDouble();
                var shiftId = dice switch
                {
                    < 0.75 => dayShift.Id,
                    < 0.95 => flex.Id,
                    _ => night.Id
                };
                schedule.Add(Schedule.Create(tenantId, employee.Id, shiftId, date, false));
            }
        }

        return schedule;
    }

    private static List<AttendanceRecord> CreateAttendanceRecords(
        Guid tenantId,
        List<Employee> scheduledEmployees,
        List<Schedule> schedules,
        List<Shift> shifts,
        Random random,
        DateTime nowUtc)
    {
        var shiftById = shifts.ToDictionary(s => s.Id, s => s);
        var scheduleLookup = schedules
            .Where(s => !s.IsRestDay)
            .GroupBy(s => (s.EmployeeId, s.ScheduleDate))
            .ToDictionary(g => g.Key, g => g.First());

        var records = new List<AttendanceRecord>();

        foreach (var employee in scheduledEmployees)
        {
            var employeeSchedules = schedules.Where(s => s.EmployeeId == employee.Id).ToList();
            foreach (var s in employeeSchedules)
            {
                if (s.IsRestDay) continue;

                var record = AttendanceRecord.Create(tenantId, employee.Id, s.ScheduleDate, s.Id);
                var dice = random.NextDouble();

                if (dice < 0.08)
                {
                    records.Add(record);
                    continue;
                }

                var shift = shiftById[s.ShiftId];
                var localDate = DateTime.SpecifyKind(s.ScheduleDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), DateTimeKind.Local);
                var workStart = localDate.AddHours(shift.StartTime.Hour).AddMinutes(shift.StartTime.Minute);

                var checkInDelayMinutes = (int)Math.Round(ClampNormal(random, 3, 12, -10, 90));
                var checkIn = workStart.AddMinutes(checkInDelayMinutes);

                record.CheckIn(checkIn.ToUniversalTime(), CheckInMethod.App, "31.2304,121.4737");

                if (dice < 0.18)
                {
                    records.Add(record);
                    continue;
                }

                var workEnd = localDate.AddHours(shift.EndTime.Hour).AddMinutes(shift.EndTime.Minute);
                if (shift.CrossDay) workEnd = workEnd.AddDays(1);

                var checkoutDeltaMinutes = (int)Math.Round(ClampNormal(random, -2, 20, -120, 180));
                var checkOut = workEnd.AddMinutes(checkoutDeltaMinutes);
                record.CheckOut(checkOut.ToUniversalTime(), CheckInMethod.App, "31.2304,121.4737");

                if (checkOut > checkIn)
                {
                    SetPrivateProperty(record, "OvertimeHours", (decimal)Math.Max(0, (checkOut - workEnd).TotalHours));
                }

                records.Add(record);
            }
        }

        var corrections = records.Where(r => r.CheckInTime.HasValue && !r.CheckOutTime.HasValue).Take(8).ToList();
        foreach (var r in corrections)
        {
            r.ManualCheckOut(r.CheckInTime!.Value.AddHours(8).AddMinutes(random.Next(0, 40)), "补卡：系统异常导致签退缺失");
        }

        var approvals = records.Where(r => r.CheckInTime.HasValue && r.CheckOutTime.HasValue).Take(12).ToList();
        foreach (var r in approvals)
        {
            r.Approve(Guid.Empty);
            SetPrivateProperty(r, "ApprovedAt", nowUtc);
        }

        foreach (var record in records)
        {
            if (record.ScheduleId.HasValue && scheduleLookup.TryGetValue((record.EmployeeId, record.AttendanceDate), out var schedule))
            {
                SetPrivateProperty(record, "Schedule", schedule);
            }
        }

        return records;
    }

    private static LeaveSeedResult CreateLeave(Guid tenantId, List<Employee> employees, Guid demoEmployeeId, Random random, DateOnly today)
    {
        var types = new[]
        {
            CreateLeaveType(tenantId, "年假", "ANNUAL", true, "#52c41a", allowHalfDay: true),
            CreateLeaveType(tenantId, "病假", "SICK", true, "#faad14", allowHalfDay: true, requiresAttachment: true),
            CreateLeaveType(tenantId, "事假", "PERSONAL", false, "#1890ff", allowHalfDay: true),
            CreateLeaveType(tenantId, "产假", "MATERNITY", true, "#eb2f96", allowHalfDay: false),
            CreateLeaveType(tenantId, "陪产假", "PATERNITY", true, "#722ed1", allowHalfDay: false),
            CreateLeaveType(tenantId, "调休", "COMP", true, "#13c2c2", allowHalfDay: true)
        };

        var policies = types.Select(t =>
        {
            var p = LeavePolicy.Create(tenantId, t.Id, $"{t.Name}规则", t.Code == "ANNUAL" ? 10m : 5m);
            SetPrivateProperty(p, "EffectiveAfterDays", t.Code == "ANNUAL" ? 90 : 0);
            SetPrivateProperty(p, "MaxCarryOverDays", t.Code == "ANNUAL" ? 5m : 0m);
            SetPrivateProperty(p, "CarryOverExpiryMonths", 6);
            SetPrivateProperty(p, "AccrualPeriod", t.Code == "ANNUAL" ? AccrualPeriod.Monthly : AccrualPeriod.Yearly);
            return p;
        }).ToList();

        var year = today.Year;
        var balances = new List<LeaveBalance>();
        foreach (var e in employees)
        {
            foreach (var t in types)
            {
                var entitlement = t.Code == "ANNUAL" ? 10m : 5m;
                var b = LeaveBalance.Create(tenantId, e.Id, t.Id, year, entitlement);
                if (t.Code == "ANNUAL" && e.HireDate < today.AddYears(-2))
                {
                    b.SetCarryOver(2m);
                }
                balances.Add(b);
            }
        }

        var balanceByKey = balances.ToDictionary(b => (b.EmployeeId, b.LeaveTypeId), b => b);

        var leaveRequests = new List<LeaveRequest>();

        var demoAnnual = types.Single(t => t.Code == "ANNUAL");
        var demoSick = types.Single(t => t.Code == "SICK");

        var r1 = LeaveRequest.Create(tenantId, demoEmployeeId, demoAnnual.Id, today.AddDays(7), today.AddDays(9), DayPart.FullDay, DayPart.FullDay, "家庭事务");
        balanceByKey[(demoEmployeeId, demoAnnual.Id)].AddPending(r1.TotalDays);
        r1.Approve(Guid.Empty, "同意");
        balanceByKey[(demoEmployeeId, demoAnnual.Id)].Use(r1.TotalDays);
        SetAttachments(r1, ["/demo-assets/leave/annual-plan.txt"]);
        leaveRequests.Add(r1);

        var r2 = LeaveRequest.Create(tenantId, demoEmployeeId, demoSick.Id, today.AddDays(-10), today.AddDays(-10), DayPart.Morning, DayPart.Morning, "就医复诊");
        balanceByKey[(demoEmployeeId, demoSick.Id)].AddPending(r2.TotalDays);
        r2.Approve(Guid.Empty, "已核对病假证明");
        balanceByKey[(demoEmployeeId, demoSick.Id)].Use(r2.TotalDays);
        SetAttachments(r2, ["/demo-assets/leave/doctor-note-001.txt"]);
        leaveRequests.Add(r2);

        var r3 = LeaveRequest.Create(tenantId, demoEmployeeId, demoAnnual.Id, today.AddDays(14), today.AddDays(14), DayPart.Afternoon, DayPart.Afternoon, "半天外出");
        balanceByKey[(demoEmployeeId, demoAnnual.Id)].AddPending(r3.TotalDays);
        leaveRequests.Add(r3);

        var otherEmployees = employees.Where(e => e.Id != demoEmployeeId).Take(80).ToList();
        foreach (var e in otherEmployees)
        {
            var t = types[random.Next(types.Length)];
            var start = today.AddDays(-random.Next(1, 120));
            var length = random.Next(0, 4);
            var end = start.AddDays(length);
            var statusRoll = random.NextDouble();
            var req = LeaveRequest.Create(tenantId, e.Id, t.Id, start, end, DayPart.FullDay, DayPart.FullDay, $"演示请假原因 {random.Next(1, 500)}");
            balanceByKey[(e.Id, t.Id)].AddPending(req.TotalDays);

            if (t.Code == "SICK" && statusRoll < 0.7)
            {
                SetAttachments(req, ["/demo-assets/leave/doctor-note-002.txt"]);
            }

            if (statusRoll < 0.55)
            {
                req.Approve(Guid.Empty, "已审批");
                balanceByKey[(e.Id, t.Id)].Use(req.TotalDays);
            }
            else if (statusRoll < 0.75)
            {
                req.Reject(Guid.Empty, "信息不完整/时间冲突");
                balanceByKey[(e.Id, t.Id)].RemovePending(req.TotalDays);
            }
            else if (statusRoll < 0.85)
            {
                req.Cancel("自行撤回");
                balanceByKey[(e.Id, t.Id)].RemovePending(req.TotalDays);
            }

            leaveRequests.Add(req);
        }

        return new LeaveSeedResult(types.ToList(), policies, balances, leaveRequests);
    }

    private static LeaveType CreateLeaveType(Guid tenantId, string name, string code, bool paid, string color, bool allowHalfDay, bool requiresAttachment = false)
    {
        var t = LeaveType.Create(tenantId, name, code, paid);
        SetPrivateProperty(t, "Color", color);
        SetPrivateProperty(t, "AllowHalfDay", allowHalfDay);
        SetPrivateProperty(t, "RequiresAttachment", requiresAttachment);
        SetPrivateProperty(t, "MaxDaysPerRequest", code == "ANNUAL" ? 10 : 5);
        SetPrivateProperty(t, "RequiresApproval", true);
        return t;
    }

    private static PayrollSeedResult CreatePayroll(Guid tenantId, List<Employee> employees, Random random, DateOnly today)
    {
        var periods = new List<PayrollPeriod>();
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        for (var i = 0; i < 12; i++)
        {
            var monthStart = start.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var payDate = monthEnd.AddDays(5);
            periods.Add(PayrollPeriod.Create(tenantId, monthStart.Year, monthStart.Month, monthStart, monthEnd, payDate));
        }

        var items =
            new[]
            {
                PayrollItem.Create(tenantId, "基本工资", "BASE", PayrollItemType.Earning, CalculationType.Manual),
                PayrollItem.Create(tenantId, "绩效奖金", "BONUS", PayrollItemType.Earning, CalculationType.Manual),
                PayrollItem.Create(tenantId, "交通补贴", "ALLOW_TRAVEL", PayrollItemType.Earning, CalculationType.Fixed),
                PayrollItem.Create(tenantId, "餐补", "ALLOW_MEAL", PayrollItemType.Earning, CalculationType.Fixed),
                PayrollItem.Create(tenantId, "个人社保", "SI_EMP", PayrollItemType.Deduction, CalculationType.Manual),
                PayrollItem.Create(tenantId, "个人公积金", "HF_EMP", PayrollItemType.Deduction, CalculationType.Manual),
                PayrollItem.Create(tenantId, "个税", "IIT", PayrollItemType.Tax, CalculationType.Manual)
            }.ToList();

        SetPrivateProperty(items.Single(i => i.Code == "ALLOW_TRAVEL"), "FixedAmount", 300m);
        SetPrivateProperty(items.Single(i => i.Code == "ALLOW_MEAL"), "FixedAmount", 600m);
        SetPrivateProperty(items.Single(i => i.Code == "IIT"), "IsTaxable", false);
        SetPrivateProperty(items.Single(i => i.Code == "BASE"), "IsTaxable", true);

        foreach (var item in items)
        {
            SetPrivateProperty(item, "IsActive", true);
        }

        var periodRecent = periods[^1];
        var records = new List<PayrollRecord>();

        var recentPeriods = periods.Skip(Math.Max(0, periods.Count - 3)).ToList();
        foreach (var e in employees.Take(80))
        {
            var baseSalary = random.Next(9000, 60000);
            foreach (var p in recentPeriods)
            {
                var record = PayrollRecord.Create(tenantId, e.Id, p.Id, baseSalary);

                record.AddDetail(items.Single(i => i.Code == "BASE").Id, "基本工资", PayrollItemType.Earning, baseSalary);
                if (random.NextDouble() < 0.55)
                {
                    record.AddDetail(items.Single(i => i.Code == "BONUS").Id, "绩效奖金", PayrollItemType.Earning, random.Next(500, 8000));
                }

                record.AddDetail(items.Single(i => i.Code == "ALLOW_TRAVEL").Id, "交通补贴", PayrollItemType.Earning, 300);
                record.AddDetail(items.Single(i => i.Code == "ALLOW_MEAL").Id, "餐补", PayrollItemType.Earning, 600);

                var si = Math.Round(baseSalary * 0.10m, 2);
                var hf = Math.Round(baseSalary * 0.07m, 2);
                var tax = Math.Round(Math.Max(0, (baseSalary - 5000m) * 0.03m), 2);

                record.AddDetail(items.Single(i => i.Code == "SI_EMP").Id, "个人社保", PayrollItemType.Deduction, si);
                record.AddDetail(items.Single(i => i.Code == "HF_EMP").Id, "个人公积金", PayrollItemType.Deduction, hf);
                record.AddDetail(items.Single(i => i.Code == "IIT").Id, "个税", PayrollItemType.Tax, tax);

                if (random.NextDouble() < 0.85)
                {
                    record.Calculate();
                    if (random.NextDouble() < 0.75)
                    {
                        record.Approve();
                    }
                    if (random.NextDouble() < 0.5)
                    {
                        record.MarkAsPaid();
                    }
                }

                records.Add(record);
            }
        }

        return new PayrollSeedResult(periods, items, records, periodRecent.Id);
    }

    private static List<ExpenseRequest> CreateExpenses(Guid tenantId, List<Employee> employees, Guid recentPayrollPeriodId, Random random, DateOnly today)
    {
        var expenseTypes = GetExpenseTypeDefinitions();
        var expenses = new List<ExpenseRequest>();

        var sourceEmployees = employees.Take(90).ToList();
        for (var i = 0; i < 220; i++)
        {
            var e = sourceEmployees[random.Next(sourceEmployees.Count)];
            var type = expenseTypes[random.Next(expenseTypes.Count)];

            var title = $"报销申请 {type.Name} #{i + 1}";
            var req = ExpenseRequest.Create(tenantId, e.Id, title);

            var amount = Math.Round((decimal)(random.NextDouble() * 3800 + 50), 2);
            var expenseDate = today.AddDays(-random.Next(0, 120));
            var receiptUrl = type.RequiresReceipt ? $"/demo-assets/receipts/receipt-{(i % 3) + 1:000}.svg" : null;

            req.AddItem(type.Category, amount, expenseDate, $"明细：{type.Name} {random.Next(1, 50)}", receiptUrl);

            if (random.NextDouble() < 0.12)
            {
                expenses.Add(req);
                continue;
            }

            req.Submit();

            var roll = random.NextDouble();
            if (roll < 0.55)
            {
                req.Approve(Guid.Empty, "通过");
            }
            else if (roll < 0.75)
            {
                req.Reject(Guid.Empty, "不符合报销规范");
            }
            else if (roll < 0.9)
            {
                req.MarkAsPaid(recentPayrollPeriodId);
            }

            expenses.Add(req);
        }

        var demoEmployee = employees.FirstOrDefault(e => e.EmployeeNumber == "EMP3001");
        if (demoEmployee != null)
        {
            var type = expenseTypes.First(t => t.Code == "TRAVEL");
            var req = ExpenseRequest.Create(tenantId, demoEmployee.Id, "演示：客户拜访差旅报销");
            req.AddItem(type.Category, 1860.50m, today.AddDays(-6), "高铁票 + 住宿", "/demo-assets/receipts/receipt-001.svg");
            req.Submit();
            expenses.Add(req);
        }

        return expenses;
    }

    private static List<AuditLog> CreateAuditLogs(Guid tenantId, Guid? userId, DateTime nowUtc)
    {
        var logs = new List<AuditLog>();
        for (var i = 0; i < 80; i++)
        {
            var action = (i % 6) switch
            {
                0 => AuditActions.Create,
                1 => AuditActions.Update,
                2 => AuditActions.Delete,
                3 => AuditActions.Login,
                4 => AuditActions.Export,
                _ => AuditActions.Import
            };
            var entityType = (i % 5) switch
            {
                0 => nameof(Employee),
                1 => nameof(LeaveRequest),
                2 => nameof(AttendanceRecord),
                3 => nameof(PayrollRecord),
                _ => nameof(ExpenseRequest)
            };
            var log = AuditLog.Create(tenantId, userId, DemoAdminUsername, action, entityType, Guid.NewGuid().ToString());
            log.SetRequestInfo("127.0.0.1", "demo-seed", $"/api/{entityType.ToLowerInvariant()}", Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            SetPrivateProperty(log, "Timestamp", nowUtc.AddMinutes(-i * 17));
            logs.Add(log);
        }
        return logs;
    }

    private static void SetAttachments(LeaveRequest request, IReadOnlyList<string> urls)
    {
        var json = JsonSerializer.Serialize(urls);
        SetPrivateProperty(request, nameof(LeaveRequest.Attachments), json);
    }

    private static double ClampNormal(Random random, double mean, double stdDev, double min, double max)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        var value = mean + stdDev * randStdNormal;
        return Math.Clamp(value, min, max);
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object? value)
    {
        if (instance == null) return;
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null) return;
        var setter = property.GetSetMethod(true);
        if (setter == null) return;
        setter.Invoke(instance, [value]);
    }

    private record DateRange(DateOnly Start, DateOnly End);

    public record ExpenseTypeDefinition(Guid Id, string Code, string Name, ExpenseCategory Category, bool RequiresReceipt, bool RequiresApproval, decimal? MaxAmount);

    private record OrganizationSeedResult(OrganizationUnit Company, OrganizationUnit[] Divisions, OrganizationUnit[] Departments, List<OrganizationUnit> AllUnits);

    private sealed class EmployeeSeedResult(List<Employee> allEmployees, List<JobHistory> allJobHistories, List<Employee> scheduledEmployees, DemoUsersIds demoUsers)
    {
        public List<Employee> AllEmployees { get; } = allEmployees;
        public List<JobHistory> AllJobHistories { get; } = allJobHistories;
        public List<Employee> ScheduledEmployees { get; } = scheduledEmployees;
        public DemoUsersIds DemoUsers { get; } = demoUsers;
    }

    private record DemoUsersEmployees(
        Employee AdminEmployee,
        Employee HrEmployee,
        Employee RegularEmployee,
        Guid AdminDepartmentId,
        Guid HrDepartmentId,
        Guid RegularDepartmentId,
        Guid AdminPositionId,
        Guid HrPositionId,
        Guid RegularPositionId,
        DemoUsersIds Users);

    private sealed class DemoUsersIds(Guid employeeId, Guid hrEmployeeId, Guid regularEmployeeId)
    {
        public Guid EmployeeId { get; } = employeeId;
        public Guid HrEmployeeId { get; } = hrEmployeeId;
        public Guid RegularEmployeeId { get; } = regularEmployeeId;
        public Guid? DemoAdminUserId { get; set; }
        public Guid? DemoHrUserId { get; set; }
        public Guid? DemoUserId { get; set; }
    }

    private record LeaveSeedResult(List<LeaveType> LeaveTypes, List<LeavePolicy> LeavePolicies, List<LeaveBalance> LeaveBalances, List<LeaveRequest> LeaveRequests);

    private record PayrollSeedResult(List<PayrollPeriod> PayrollPeriods, List<PayrollItem> PayrollItems, List<PayrollRecord> PayrollRecords, Guid RecentPeriodId);
}
