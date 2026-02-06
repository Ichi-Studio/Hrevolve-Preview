namespace Hrevolve.Agent.Configuration;

/// <summary>
/// Text2SQL 配置选项
/// </summary>
public class Text2SqlOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "Text2Sql";
    
    /// <summary>是否启用 Text2SQL 功能</summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>是否启用 CRUD 操作（INSERT/UPDATE/DELETE）</summary>
    public bool EnableCrud { get; set; } = true;
    
    /// <summary>最大 JOIN 表数量</summary>
    public int MaxJoinTables { get; set; } = 5;
    
    /// <summary>最大过滤条件数量</summary>
    public int MaxFilters { get; set; } = 20;
    
    /// <summary>最大返回行数</summary>
    public int MaxResultRows { get; set; } = 1000;
    
    /// <summary>默认返回行数</summary>
    public int DefaultResultRows { get; set; } = 100;
    
    /// <summary>查询超时时间（秒）</summary>
    public int QueryTimeoutSeconds { get; set; } = 30;
    
    /// <summary>是否启用查询缓存</summary>
    public bool EnableQueryCache { get; set; } = true;
    
    /// <summary>缓存有效期（分钟）</summary>
    public int CacheDurationMinutes { get; set; } = 5;
    
    /// <summary>允许访问的实体列表</summary>
    public List<string> AllowedEntities { get; set; } =
    [
        "Employee",
        "AttendanceRecord",
        "LeaveRequest",
        "LeaveBalance",
        "LeaveType",
        "PayrollRecord",
        "OrganizationUnit",
        "Position"
    ];
    
    /// <summary>危险关键字黑名单</summary>
    public List<string> BlockedKeywords { get; set; } =
    [
        "exec",
        "execute",
        "xp_",
        "sp_",
        "sys.",
        "information_schema",
        "--",
        "/*",
        "*/",
        ";--",
        "drop",
        "truncate",
        "alter"
    ];
    
    /// <summary>敏感字段配置（实体名 -> 敏感字段列表，"*" 表示所有字段）</summary>
    public Dictionary<string, List<string>> SensitiveFields { get; set; } = new()
    {
        ["Employee"] = ["IdCardNumber", "PersonalEmail"],
        ["PayrollRecord"] = ["*"],
        ["Position"] = ["SalaryRangeMin", "SalaryRangeMax"]
    };
    
    /// <summary>执行 CRUD 操作所需的权限</summary>
    public Dictionary<string, string> CrudPermissions { get; set; } = new()
    {
        ["Insert"] = "hr:write",
        ["Update"] = "hr:write",
        ["Delete"] = "hr:admin"
    };
    
    /// <summary>查询复杂度限制分数（超过则拒绝）</summary>
    public int MaxComplexityScore { get; set; } = 50;
}
