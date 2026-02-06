namespace Hrevolve.Agent.Models.Text2Sql;

/// <summary>
/// 查询操作类型
/// </summary>
public enum QueryOperation
{
    /// <summary>查询数据</summary>
    Select,
    /// <summary>插入数据</summary>
    Insert,
    /// <summary>更新数据</summary>
    Update,
    /// <summary>删除数据</summary>
    Delete
}

/// <summary>
/// 聚合类型
/// </summary>
public enum AggregationType
{
    /// <summary>计数</summary>
    Count,
    /// <summary>求和</summary>
    Sum,
    /// <summary>平均值</summary>
    Avg,
    /// <summary>最小值</summary>
    Min,
    /// <summary>最大值</summary>
    Max,
    /// <summary>去重计数</summary>
    CountDistinct
}

/// <summary>
/// 过滤条件操作符
/// </summary>
public enum FilterOperator
{
    /// <summary>等于</summary>
    Equal,
    /// <summary>不等于</summary>
    NotEqual,
    /// <summary>大于</summary>
    GreaterThan,
    /// <summary>大于等于</summary>
    GreaterThanOrEqual,
    /// <summary>小于</summary>
    LessThan,
    /// <summary>小于等于</summary>
    LessThanOrEqual,
    /// <summary>包含</summary>
    Contains,
    /// <summary>以...开头</summary>
    StartsWith,
    /// <summary>以...结尾</summary>
    EndsWith,
    /// <summary>在列表中</summary>
    In,
    /// <summary>不在列表中</summary>
    NotIn,
    /// <summary>为空</summary>
    IsNull,
    /// <summary>不为空</summary>
    IsNotNull,
    /// <summary>日期范围</summary>
    Between
}

/// <summary>
/// 过滤条件
/// </summary>
public class FilterCondition
{
    /// <summary>字段名（支持导航属性，如 Employee.FirstName）</summary>
    public string Field { get; set; } = string.Empty;
    
    /// <summary>操作符</summary>
    public FilterOperator Operator { get; set; }
    
    /// <summary>值（对于 Between 操作符，使用逗号分隔的两个值）</summary>
    public object? Value { get; set; }
    
    /// <summary>逻辑运算符（AND/OR），与下一个条件的连接方式</summary>
    public string LogicalOperator { get; set; } = "AND";
}

/// <summary>
/// JOIN 子句
/// </summary>
public class JoinClause
{
    /// <summary>要连接的实体名称</summary>
    public string Entity { get; set; } = string.Empty;
    
    /// <summary>连接条件（如 "EmployeeId = Employee.Id"）</summary>
    public string On { get; set; } = string.Empty;
    
    /// <summary>连接类型（Inner, Left, Right）</summary>
    public string JoinType { get; set; } = "Inner";
    
    /// <summary>别名</summary>
    public string? Alias { get; set; }
}

/// <summary>
/// 排序条件
/// </summary>
public class OrderByClause
{
    /// <summary>排序字段</summary>
    public string Field { get; set; } = string.Empty;
    
    /// <summary>是否降序</summary>
    public bool Descending { get; set; }
}

/// <summary>
/// 结构化查询请求
/// </summary>
public class QueryRequest
{
    /// <summary>操作类型</summary>
    public QueryOperation Operation { get; set; } = QueryOperation.Select;
    
    /// <summary>目标实体（Employee, AttendanceRecord 等）</summary>
    public string TargetEntity { get; set; } = string.Empty;
    
    /// <summary>选择的字段列表（为空则选择所有允许的字段）</summary>
    public List<string> SelectFields { get; set; } = [];
    
    /// <summary>过滤条件列表</summary>
    public List<FilterCondition> Filters { get; set; } = [];
    
    /// <summary>JOIN 子句列表</summary>
    public List<JoinClause> Joins { get; set; } = [];
    
    /// <summary>聚合类型（仅用于聚合查询）</summary>
    public AggregationType? Aggregation { get; set; }
    
    /// <summary>聚合字段（用于 Sum, Avg 等）</summary>
    public string? AggregationField { get; set; }
    
    /// <summary>分组字段（用于 GROUP BY）</summary>
    public List<string> GroupByFields { get; set; } = [];
    
    /// <summary>排序条件列表</summary>
    public List<OrderByClause> OrderBy { get; set; } = [];
    
    /// <summary>返回行数限制（默认100，最大1000）</summary>
    public int Limit { get; set; } = 100;
    
    /// <summary>跳过行数（用于分页）</summary>
    public int Offset { get; set; }
    
    /// <summary>要更新或插入的值（用于 Insert/Update 操作）</summary>
    public Dictionary<string, object?> UpdateValues { get; set; } = [];
    
    /// <summary>原始自然语言查询（用于日志和调试）</summary>
    public string? OriginalQuery { get; set; }
}
