namespace Hrevolve.Agent.Models.Text2Sql;

/// <summary>
/// 查询结果
/// </summary>
public class QueryResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>错误消息（如果失败）</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>错误代码</summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>查询数据（对于 SELECT 操作）</summary>
    public List<Dictionary<string, object?>> Data { get; set; } = [];
    
    /// <summary>返回的行数</summary>
    public int RowCount { get; set; }
    
    /// <summary>总行数（不考虑分页限制）</summary>
    public int? TotalCount { get; set; }
    
    /// <summary>聚合结果（对于聚合查询）</summary>
    public object? AggregationResult { get; set; }
    
    /// <summary>受影响的行数（对于 Insert/Update/Delete 操作）</summary>
    public int? AffectedRows { get; set; }
    
    /// <summary>新插入记录的 ID（对于 Insert 操作）</summary>
    public Guid? InsertedId { get; set; }
    
    /// <summary>执行的操作类型</summary>
    public QueryOperation Operation { get; set; }
    
    /// <summary>查询执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>列信息（字段名和类型）</summary>
    public List<ColumnInfo> Columns { get; set; } = [];

    /// <summary>生成的 SQL（可选）</summary>
    public string? GeneratedSql { get; set; }

    /// <summary>告警信息（可选）</summary>
    public List<string> Warnings { get; set; } = [];
    
    /// <summary>创建成功结果</summary>
    public static QueryResult Ok(List<Dictionary<string, object?>> data, int rowCount, List<ColumnInfo>? columns = null) => new()
    {
        Success = true,
        Data = data,
        RowCount = rowCount,
        Operation = QueryOperation.Select,
        Columns = columns ?? []
    };
    
    /// <summary>创建聚合查询成功结果</summary>
    public static QueryResult OkAggregation(object? result) => new()
    {
        Success = true,
        AggregationResult = result,
        Operation = QueryOperation.Select
    };
    
    /// <summary>创建修改操作成功结果</summary>
    public static QueryResult OkModified(QueryOperation operation, int affectedRows, Guid? insertedId = null) => new()
    {
        Success = true,
        Operation = operation,
        AffectedRows = affectedRows,
        InsertedId = insertedId
    };
    
    /// <summary>创建失败结果</summary>
    public static QueryResult Fail(string errorMessage, string? errorCode = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}

/// <summary>
/// 列信息
/// </summary>
public class ColumnInfo
{
    /// <summary>列名</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>显示名称（中文）</summary>
    public string? DisplayName { get; set; }
    
    /// <summary>数据类型</summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>是否可为空</summary>
    public bool IsNullable { get; set; }
}
