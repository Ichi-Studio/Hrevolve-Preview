using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrevolve.Web.Controllers;

/// <summary>
/// 多语言本地化控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class LocalizationController : ControllerBase
{
    private static readonly string[] SupportedLocales = ["zh-CN", "zh-TW", "en-US"];
    
    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    [HttpGet("locales")]
    public IActionResult GetSupportedLocales()
    {
        var locales = new[]
        {
            new { code = "zh-CN", name = "简体中文", flag = "🇨🇳" },
            new { code = "zh-TW", name = "繁體中文", flag = "🇹🇼" },
            new { code = "en-US", name = "English", flag = "🇺🇸" }
        };
        return Ok(locales);
    }
    
    /// <summary>
    /// 获取指定语言的翻译包
    /// </summary>
    [HttpGet("messages/{locale}")]
    public IActionResult GetMessages(string locale)
    {
        if (!SupportedLocales.Contains(locale))
        {
            return BadRequest(new { message = $"不支持的语言: {locale}" });
        }
        
        var messages = locale switch
        {
            "zh-CN" => GetZhCNMessages(),
            "zh-TW" => GetZhTWMessages(),
            "en-US" => GetEnUSMessages(),
            _ => GetZhCNMessages()
        };
        
        return Ok(messages);
    }
    
    /// <summary>
    /// 批量获取所有语言包
    /// </summary>
    [HttpGet("messages")]
    public IActionResult GetAllMessages()
    {
        var allMessages = new Dictionary<string, object>
        {
            ["zh-CN"] = GetZhCNMessages(),
            ["zh-TW"] = GetZhTWMessages(),
            ["en-US"] = GetEnUSMessages()
        };
        return Ok(allMessages);
    }
    
    private static object GetZhCNMessages() => new
    {
        common = new
        {
            confirm = "确认",
            cancel = "取消",
            save = "保存",
            delete = "删除",
            edit = "编辑",
            add = "新增",
            search = "搜索",
            reset = "重置",
            loading = "加载中...",
            noData = "暂无数据",
            success = "操作成功",
            failed = "操作失败",
            required = "必填项",
            actions = "操作",
            status = "状态",
            remark = "备注",
            createdAt = "创建时间",
            updatedAt = "更新时间",
            settings = "设置"
        },
        auth = new
        {
            login = "登录",
            logout = "退出登录",
            username = "用户名",
            password = "密码",
            rememberMe = "记住我",
            forgotPassword = "忘记密码？",
            loginSuccess = "登录成功",
            loginFailed = "登录失败",
            pleaseInputUsername = "请输入用户名",
            pleaseInputPassword = "请输入密码"
        },
        menu = new
        {
            dashboard = "工作台",
            selfService = "员工自助",
            profile = "个人信息",
            myAttendance = "我的考勤",
            myLeave = "我的假期",
            myPayroll = "我的薪资",
            assistant = "AI助手",
            organization = "组织管理",
            orgStructure = "组织架构",
            positions = "职位管理",
            employees = "员工管理",
            employeeList = "员工列表",
            attendance = "考勤管理",
            attendanceRecords = "考勤记录",
            shifts = "班次管理",
            leave = "假期管理",
            leaveRequests = "请假申请",
            leaveApprovals = "假期审批",
            leaveTypes = "假期类型",
            payroll = "薪酬管理",
            payrollRecords = "薪资记录",
            payrollPeriods = "薪资周期"
        },
        dashboard_page = new
        {
            welcome = "欢迎回来",
            todayAttendance = "今日考勤",
            leaveBalance = "假期余额",
            pendingApprovals = "待审批",
            teamMembers = "团队成员",
            checkIn = "签到",
            checkOut = "签退",
            @checked = "已打卡",
            notChecked = "未打卡"
        },
        employee = new
        {
            employeeNo = "工号",
            name = "姓名",
            email = "邮箱",
            phone = "电话",
            department = "部门",
            position = "职位",
            manager = "直属上级",
            hireDate = "入职日期",
            status = "状态",
            statusActive = "在职",
            statusOnLeave = "休假中",
            statusTerminated = "已离职",
            statusProbation = "试用期"
        },
        attendance_page = new
        {
            date = "日期",
            shift = "班次",
            checkInTime = "签到时间",
            checkOutTime = "签退时间",
            workHours = "工作时长",
            overtimeHours = "加班时长",
            statusNormal = "正常",
            statusLate = "迟到",
            statusEarlyLeave = "早退",
            statusAbsent = "缺勤",
            statusLeave = "请假",
            statusHoliday = "节假日"
        },
        leave_page = new
        {
            leaveType = "假期类型",
            startDate = "开始日期",
            endDate = "结束日期",
            days = "天数",
            reason = "请假原因",
            approver = "审批人",
            approvalStatus = "审批状态",
            statusPending = "待审批",
            statusApproved = "已批准",
            statusRejected = "已拒绝",
            statusCancelled = "已取消",
            annualLeave = "年假",
            sickLeave = "病假",
            personalLeave = "事假",
            maternityLeave = "产假",
            compensatoryLeave = "调休假",
            balance = "余额",
            used = "已用",
            remaining = "剩余"
        },
        payroll_page = new
        {
            period = "薪资周期",
            baseSalary = "基本工资",
            bonus = "奖金",
            allowances = "津贴",
            deductions = "扣款",
            socialInsurance = "社保",
            housingFund = "公积金",
            tax = "个税",
            netSalary = "实发工资",
            statusDraft = "草稿",
            statusCalculated = "已计算",
            statusApproved = "已审批",
            statusPaid = "已发放"
        },
        assistant_page = new
        {
            title = "HR智能助手",
            placeholder = "请输入您的问题，例如：我还有多少年假？",
            send = "发送",
            thinking = "正在思考...",
            clearHistory = "清空对话",
            suggestions = "您可以问我：",
            suggestion1 = "我还有多少年假？",
            suggestion2 = "帮我请假",
            suggestion3 = "查询本月薪资",
            suggestion4 = "今天的考勤状态"
        }
    };

    
    private static object GetZhTWMessages() => new
    {
        common = new
        {
            confirm = "確認",
            cancel = "取消",
            save = "儲存",
            delete = "刪除",
            edit = "編輯",
            add = "新增",
            search = "搜尋",
            reset = "重置",
            loading = "載入中...",
            noData = "暫無資料",
            success = "操作成功",
            failed = "操作失敗",
            required = "必填項",
            actions = "操作",
            status = "狀態",
            remark = "備註",
            createdAt = "建立時間",
            updatedAt = "更新時間",
            settings = "設定"
        },
        auth = new
        {
            login = "登入",
            logout = "登出",
            username = "使用者名稱",
            password = "密碼",
            rememberMe = "記住我",
            forgotPassword = "忘記密碼？",
            loginSuccess = "登入成功",
            loginFailed = "登入失敗",
            pleaseInputUsername = "請輸入使用者名稱",
            pleaseInputPassword = "請輸入密碼"
        },
        menu = new
        {
            dashboard = "工作台",
            selfService = "員工自助",
            profile = "個人資訊",
            myAttendance = "我的考勤",
            myLeave = "我的假期",
            myPayroll = "我的薪資",
            assistant = "AI助手",
            organization = "組織管理",
            orgStructure = "組織架構",
            positions = "職位管理",
            employees = "員工管理",
            employeeList = "員工列表",
            attendance = "考勤管理",
            attendanceRecords = "考勤記錄",
            shifts = "班次管理",
            leave = "假期管理",
            leaveRequests = "請假申請",
            leaveApprovals = "假期審批",
            leaveTypes = "假期類型",
            payroll = "薪酬管理",
            payrollRecords = "薪資記錄",
            payrollPeriods = "薪資週期"
        },
        dashboard_page = new
        {
            welcome = "歡迎回來",
            todayAttendance = "今日考勤",
            leaveBalance = "假期餘額",
            pendingApprovals = "待審批",
            teamMembers = "團隊成員",
            checkIn = "簽到",
            checkOut = "簽退",
            @checked = "已打卡",
            notChecked = "未打卡"
        },
        employee = new
        {
            employeeNo = "工號",
            name = "姓名",
            email = "電子郵件",
            phone = "電話",
            department = "部門",
            position = "職位",
            manager = "直屬主管",
            hireDate = "入職日期",
            status = "狀態",
            statusActive = "在職",
            statusOnLeave = "休假中",
            statusTerminated = "已離職",
            statusProbation = "試用期"
        },
        attendance_page = new
        {
            date = "日期",
            shift = "班次",
            checkInTime = "簽到時間",
            checkOutTime = "簽退時間",
            workHours = "工作時數",
            overtimeHours = "加班時數",
            statusNormal = "正常",
            statusLate = "遲到",
            statusEarlyLeave = "早退",
            statusAbsent = "缺勤",
            statusLeave = "請假",
            statusHoliday = "節假日"
        },
        leave_page = new
        {
            leaveType = "假期類型",
            startDate = "開始日期",
            endDate = "結束日期",
            days = "天數",
            reason = "請假原因",
            approver = "審批人",
            approvalStatus = "審批狀態",
            statusPending = "待審批",
            statusApproved = "已批准",
            statusRejected = "已拒絕",
            statusCancelled = "已取消",
            annualLeave = "年假",
            sickLeave = "病假",
            personalLeave = "事假",
            maternityLeave = "產假",
            compensatoryLeave = "調休假",
            balance = "餘額",
            used = "已用",
            remaining = "剩餘"
        },
        payroll_page = new
        {
            period = "薪資週期",
            baseSalary = "基本薪資",
            bonus = "獎金",
            allowances = "津貼",
            deductions = "扣款",
            socialInsurance = "社保",
            housingFund = "公積金",
            tax = "個稅",
            netSalary = "實發薪資",
            statusDraft = "草稿",
            statusCalculated = "已計算",
            statusApproved = "已審批",
            statusPaid = "已發放"
        },
        assistant_page = new
        {
            title = "HR智慧助手",
            placeholder = "請輸入您的問題，例如：我還有多少年假？",
            send = "發送",
            thinking = "正在思考...",
            clearHistory = "清空對話",
            suggestions = "您可以問我：",
            suggestion1 = "我還有多少年假？",
            suggestion2 = "幫我請假",
            suggestion3 = "查詢本月薪資",
            suggestion4 = "今天的考勤狀態"
        }
    };
    
    private static object GetEnUSMessages() => new
    {
        common = new
        {
            confirm = "Confirm",
            cancel = "Cancel",
            save = "Save",
            delete = "Delete",
            edit = "Edit",
            add = "Add",
            search = "Search",
            reset = "Reset",
            loading = "Loading...",
            noData = "No Data",
            success = "Success",
            failed = "Failed",
            required = "Required",
            actions = "Actions",
            status = "Status",
            remark = "Remark",
            createdAt = "Created At",
            updatedAt = "Updated At",
            settings = "Settings"
        },
        auth = new
        {
            login = "Login",
            logout = "Logout",
            username = "Username",
            password = "Password",
            rememberMe = "Remember Me",
            forgotPassword = "Forgot Password?",
            loginSuccess = "Login Successful",
            loginFailed = "Login Failed",
            pleaseInputUsername = "Please enter username",
            pleaseInputPassword = "Please enter password"
        },
        menu = new
        {
            dashboard = "Dashboard",
            selfService = "Self Service",
            profile = "My Profile",
            myAttendance = "My Attendance",
            myLeave = "My Leave",
            myPayroll = "My Payroll",
            assistant = "AI Assistant",
            organization = "Organization",
            orgStructure = "Org Structure",
            positions = "Positions",
            employees = "Employees",
            employeeList = "Employee List",
            attendance = "Attendance",
            attendanceRecords = "Attendance Records",
            shifts = "Shifts",
            leave = "Leave",
            leaveRequests = "Leave Requests",
            leaveApprovals = "Leave Approvals",
            leaveTypes = "Leave Types",
            payroll = "Payroll",
            payrollRecords = "Payroll Records",
            payrollPeriods = "Payroll Periods"
        },
        dashboard_page = new
        {
            welcome = "Welcome Back",
            todayAttendance = "Today Attendance",
            leaveBalance = "Leave Balance",
            pendingApprovals = "Pending Approvals",
            teamMembers = "Team Members",
            checkIn = "Check In",
            checkOut = "Check Out",
            @checked = "Checked",
            notChecked = "Not Checked"
        },
        employee = new
        {
            employeeNo = "Employee No.",
            name = "Name",
            email = "Email",
            phone = "Phone",
            department = "Department",
            position = "Position",
            manager = "Manager",
            hireDate = "Hire Date",
            status = "Status",
            statusActive = "Active",
            statusOnLeave = "On Leave",
            statusTerminated = "Terminated",
            statusProbation = "Probation"
        },
        attendance_page = new
        {
            date = "Date",
            shift = "Shift",
            checkInTime = "Check In",
            checkOutTime = "Check Out",
            workHours = "Work Hours",
            overtimeHours = "Overtime",
            statusNormal = "Normal",
            statusLate = "Late",
            statusEarlyLeave = "Early Leave",
            statusAbsent = "Absent",
            statusLeave = "On Leave",
            statusHoliday = "Holiday"
        },
        leave_page = new
        {
            leaveType = "Leave Type",
            startDate = "Start Date",
            endDate = "End Date",
            days = "Days",
            reason = "Reason",
            approver = "Approver",
            approvalStatus = "Status",
            statusPending = "Pending",
            statusApproved = "Approved",
            statusRejected = "Rejected",
            statusCancelled = "Cancelled",
            annualLeave = "Annual Leave",
            sickLeave = "Sick Leave",
            personalLeave = "Personal Leave",
            maternityLeave = "Maternity Leave",
            compensatoryLeave = "Compensatory Leave",
            balance = "Balance",
            used = "Used",
            remaining = "Remaining"
        },
        payroll_page = new
        {
            period = "Period",
            baseSalary = "Base Salary",
            bonus = "Bonus",
            allowances = "Allowances",
            deductions = "Deductions",
            socialInsurance = "Social Insurance",
            housingFund = "Housing Fund",
            tax = "Tax",
            netSalary = "Net Salary",
            statusDraft = "Draft",
            statusCalculated = "Calculated",
            statusApproved = "Approved",
            statusPaid = "Paid"
        },
        assistant_page = new
        {
            title = "HR Assistant",
            placeholder = "Ask me anything, e.g., How many annual leave days do I have?",
            send = "Send",
            thinking = "Thinking...",
            clearHistory = "Clear History",
            suggestions = "You can ask me:",
            suggestion1 = "How many annual leave days do I have?",
            suggestion2 = "Help me apply for leave",
            suggestion3 = "Check my salary this month",
            suggestion4 = "My attendance status today"
        }
    };
}
