namespace Hrevolve.Agent.Models.Text2Sql;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>是否验证通过</summary>
    public bool IsValid { get; set; }
    
    /// <summary>验证错误列表</summary>
    public List<ValidationError> Errors { get; set; } = [];
    
    /// <summary>验证警告列表</summary>
    public List<ValidationWarning> Warnings { get; set; } = [];
    
    /// <summary>修正后的查询请求（如果进行了自动修正）</summary>
    public QueryRequest? CorrectedRequest { get; set; }
    
    /// <summary>创建成功结果</summary>
    public static ValidationResult Ok() => new() { IsValid = true };
    
    /// <summary>创建成功结果（带警告）</summary>
    public static ValidationResult OkWithWarnings(params ValidationWarning[] warnings) => new()
    {
        IsValid = true,
        Warnings = [..warnings]
    };
    
    /// <summary>创建失败结果</summary>
    public static ValidationResult Fail(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = [..errors]
    };
    
    /// <summary>创建失败结果（单个错误）</summary>
    public static ValidationResult Fail(string errorCode, string message) => new()
    {
        IsValid = false,
        Errors = [new ValidationError { Code = errorCode, Message = message }]
    };
    
    /// <summary>合并多个验证结果</summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var combined = new ValidationResult { IsValid = true };
        
        foreach (var result in results)
        {
            if (!result.IsValid)
            {
                combined.IsValid = false;
            }
            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);
        }
        
        return combined;
    }
}

/// <summary>
/// 验证错误
/// </summary>
public class ValidationError
{
    /// <summary>错误代码</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>错误消息</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>相关字段（如果适用）</summary>
    public string? Field { get; set; }
    
    /// <summary>错误详情</summary>
    public string? Details { get; set; }
}

/// <summary>
/// 验证警告
/// </summary>
public class ValidationWarning
{
    /// <summary>警告代码</summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>警告消息</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>相关字段（如果适用）</summary>
    public string? Field { get; set; }
}

/// <summary>
/// 常用验证错误代码
/// </summary>
public static class ValidationErrorCodes
{
    /// <summary>实体不在白名单中</summary>
    public const string EntityNotAllowed = "ENTITY_NOT_ALLOWED";
    
    /// <summary>字段不在白名单中</summary>
    public const string FieldNotAllowed = "FIELD_NOT_ALLOWED";
    
    /// <summary>操作不允许</summary>
    public const string OperationNotAllowed = "OPERATION_NOT_ALLOWED";
    
    /// <summary>查询过于复杂</summary>
    public const string QueryTooComplex = "QUERY_TOO_COMPLEX";
    
    /// <summary>超过 JOIN 限制</summary>
    public const string TooManyJoins = "TOO_MANY_JOINS";
    
    /// <summary>超过过滤条件限制</summary>
    public const string TooManyFilters = "TOO_MANY_FILTERS";
    
    /// <summary>超过结果集大小限制</summary>
    public const string ResultSetTooLarge = "RESULT_SET_TOO_LARGE";
    
    /// <summary>包含危险关键字</summary>
    public const string DangerousKeyword = "DANGEROUS_KEYWORD";
    
    /// <summary>权限不足</summary>
    public const string InsufficientPermission = "INSUFFICIENT_PERMISSION";
    
    /// <summary>敏感字段访问被拒绝</summary>
    public const string SensitiveFieldDenied = "SENSITIVE_FIELD_DENIED";
    
    /// <summary>数据范围超出权限</summary>
    public const string DataScopeExceeded = "DATA_SCOPE_EXCEEDED";
    
    /// <summary>查询解析失败</summary>
    public const string QueryParseFailed = "QUERY_PARSE_FAILED";
    
    /// <summary>无效的过滤条件</summary>
    public const string InvalidFilter = "INVALID_FILTER";
    
    /// <summary>无效的字段类型</summary>
    public const string InvalidFieldType = "INVALID_FIELD_TYPE";
}
