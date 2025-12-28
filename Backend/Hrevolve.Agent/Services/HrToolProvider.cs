namespace Hrevolve.Agent.Services;

/// <summary>
/// HR工具提供者接口
/// </summary>
public interface IHrToolProvider
{
    /// <summary>
    /// 获取所有可用的AI工具
    /// </summary>
    IList<AITool> GetTools();
}

/// <summary>
/// HR工具提供者实现 - 提供AI Agent可调用的工具函数
/// </summary>
public class HrToolProvider(IServiceProvider serviceProvider) : IHrToolProvider
{
    
    public IList<AITool> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(GetLeaveBalance, "get_leave_balance", "查询员工的假期余额信息"),
            AIFunctionFactory.Create(SubmitLeaveRequest, "submit_leave_request", "帮助员工提交请假申请"),
            AIFunctionFactory.Create(GetSalaryInfo, "get_salary_info", "查询员工的薪资信息"),
            AIFunctionFactory.Create(GetAttendanceRecords, "get_attendance_records", "查询员工的考勤记录"),
            AIFunctionFactory.Create(QueryHrPolicy, "query_hr_policy", "查询公司HR政策和规章制度"),
            AIFunctionFactory.Create(GetOrganizationInfo, "get_organization_info", "查询公司组织架构信息"),
            AIFunctionFactory.Create(GetTodayAttendance, "get_today_attendance", "获取今日考勤状态")
        ];
    }
    
    /// <summary>
    /// 查询假期余额
    /// </summary>
    [Description("查询员工的假期余额信息，包括年假、病假等各类假期的可用天数")]
    private static async Task<string> GetLeaveBalance(
        [Description("员工ID")] Guid employeeId,
        [Description("假期类型（可选），如：年假、病假、调休")] string? leaveType = null)
    {
        // TODO: 调用LeaveService获取假期余额
        await Task.Delay(100); // 模拟异步操作
        
        if (string.IsNullOrEmpty(leaveType))
        {
            return $"""
                员工假期余额查询结果：
                - 年假：剩余 5 天（已使用 5 天，共 10 天）
                - 病假：剩余 10 天（已使用 2 天，共 12 天）
                - 调休假：剩余 2 天
                - 婚假：剩余 10 天（未使用）
                """;
        }
        
        return $"员工 {leaveType} 余额：剩余 5 天";
    }
    
    /// <summary>
    /// 提交请假申请
    /// </summary>
    [Description("帮助员工提交请假申请")]
    private static async Task<string> SubmitLeaveRequest(
        [Description("员工ID")] Guid employeeId,
        [Description("假期类型，如：年假、病假、事假")] string leaveType,
        [Description("开始日期，格式：YYYY-MM-DD")] string startDate,
        [Description("结束日期，格式：YYYY-MM-DD")] string endDate,
        [Description("请假原因")] string reason)
    {
        // TODO: 调用LeaveService提交请假
        await Task.Delay(100);
        
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
        {
            return "日期格式错误，请使用 YYYY-MM-DD 格式";
        }
        
        var days = (end.DayNumber - start.DayNumber) + 1;
        
        return $"""
            请假申请已提交成功！
            - 假期类型：{leaveType}
            - 开始日期：{startDate}
            - 结束日期：{endDate}
            - 请假天数：{days} 天
            - 请假原因：{reason}
            - 申请状态：待审批
            
            您的直属上级将收到审批通知，请耐心等待。
            """;
    }
    
    /// <summary>
    /// 查询薪资信息
    /// </summary>
    [Description("查询员工的薪资信息")]
    private static async Task<string> GetSalaryInfo(
        [Description("员工ID")] Guid employeeId,
        [Description("年份（可选）")] int? year = null,
        [Description("月份（可选）")] int? month = null)
    {
        // TODO: 调用PayrollService获取薪资信息
        await Task.Delay(100);
        
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        return $"""
            {targetYear}年{targetMonth}月薪资明细：
            - 基本工资：¥15,000.00
            - 绩效奖金：¥3,000.00
            - 餐补：¥500.00
            - 交通补贴：¥300.00
            - 社保（个人）：-¥1,200.00
            - 公积金（个人）：-¥1,500.00
            - 个人所得税：-¥800.00
            ─────────────────
            实发工资：¥15,300.00
            
            发放日期：{targetYear}年{targetMonth}月15日
            """;
    }
    
    /// <summary>
    /// 查询考勤记录
    /// </summary>
    [Description("查询员工的考勤记录")]
    private static async Task<string> GetAttendanceRecords(
        [Description("员工ID")] Guid employeeId,
        [Description("开始日期，格式：YYYY-MM-DD")] string startDate,
        [Description("结束日期，格式：YYYY-MM-DD")] string endDate)
    {
        // TODO: 调用AttendanceService获取考勤记录
        await Task.Delay(100);
        
        return $"""
            考勤记录查询结果（{startDate} 至 {endDate}）：
            - 出勤天数：20 天
            - 迟到次数：1 次（12月5日，迟到15分钟）
            - 早退次数：0 次
            - 缺勤天数：0 天
            - 加班时长：8 小时
            
            本月考勤状态：正常
            """;
    }
    
    /// <summary>
    /// 查询HR政策
    /// </summary>
    [Description("查询公司HR政策和规章制度")]
    private static async Task<string> QueryHrPolicy(
        [Description("查询问题")] string question)
    {
        // TODO: 使用RAG从向量数据库检索相关政策文档
        await Task.Delay(100);
        
        // 简单的关键词匹配示例
        if (question.Contains("年假") || question.Contains("休假"))
        {
            return """
                【年假政策】
                根据公司规定：
                1. 入职满1年后享有年假
                2. 工龄1-5年：5天/年
                3. 工龄5-10年：10天/年
                4. 工龄10年以上：15天/年
                5. 年假可结转至次年3月底前使用
                6. 未使用的年假不予折现
                
                如需了解更多详情，请联系HR部门。
                """;
        }
        
        if (question.Contains("报销") || question.Contains("费用"))
        {
            return """
                【报销政策】
                1. 差旅费：需提前申请出差审批
                2. 餐饮费：单次不超过200元/人
                3. 交通费：优先使用公共交通
                4. 报销时限：费用发生后30天内提交
                5. 必须提供正规发票
                
                报销流程：提交申请 → 部门审批 → 财务审核 → 打款
                """;
        }
        
        return $"关于「{question}」的政策信息，建议您联系HR部门获取详细解答，或查阅公司内部知识库。";
    }
    
    /// <summary>
    /// 查询组织架构
    /// </summary>
    [Description("查询公司组织架构信息")]
    private static async Task<string> GetOrganizationInfo(
        [Description("部门名称或代码（可选）")] string? department = null)
    {
        // TODO: 调用OrganizationService获取组织信息
        await Task.Delay(100);
        
        if (string.IsNullOrEmpty(department))
        {
            return """
                公司组织架构：
                ├── 总裁办
                ├── 技术中心
                │   ├── 研发一部
                │   ├── 研发二部
                │   └── 测试部
                ├── 产品中心
                ├── 市场部
                ├── 人力资源部
                └── 财务部
                
                如需查询特定部门信息，请告诉我部门名称。
                """;
        }
        
        return $"""
            【{department}】部门信息：
            - 部门负责人：张三
            - 部门人数：25人
            - 上级部门：技术中心
            - 主要职责：负责公司核心产品研发
            """;
    }
    
    /// <summary>
    /// 获取今日考勤状态
    /// </summary>
    [Description("获取员工今日的考勤状态")]
    private static async Task<string> GetTodayAttendance(
        [Description("员工ID")] Guid employeeId)
    {
        // TODO: 调用AttendanceService获取今日考勤
        await Task.Delay(100);
        
        var now = DateTime.Now;
        
        return $"""
            今日考勤状态（{now:yyyy-MM-dd}）：
            - 签到时间：09:02
            - 签退时间：{(now.Hour >= 18 ? "18:30" : "未签退")}
            - 考勤状态：正常
            - 工作时长：{(now.Hour >= 18 ? "9小时28分钟" : "进行中...")}
            """;
    }
}
