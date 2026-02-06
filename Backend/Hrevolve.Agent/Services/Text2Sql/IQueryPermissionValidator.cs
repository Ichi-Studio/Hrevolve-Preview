using System.Security.Claims;
using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// 查询权限验证器接口 - 验证用户对数据的访问权限
/// </summary>
public interface IQueryPermissionValidator
{
    /// <summary>
    /// 验证用户对查询请求的访问权限
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <param name="user">当前用户</param>
    /// <returns>验证结果，可能包含需要添加的权限过滤条件</returns>
    Task<PermissionValidationResult> ValidateAsync(QueryRequest request, ClaimsPrincipal user);
    
    /// <summary>
    /// 检查用户是否有权访问指定实体
    /// </summary>
    bool CanAccessEntity(ClaimsPrincipal user, string entityName);
    
    /// <summary>
    /// 检查用户是否有权访问指定字段
    /// </summary>
    bool CanAccessField(ClaimsPrincipal user, string entityName, string fieldName);
    
    /// <summary>
    /// 检查用户是否有权执行指定操作
    /// </summary>
    bool CanPerformOperation(ClaimsPrincipal user, QueryOperation operation);
    
    /// <summary>
    /// 获取用户可访问的字段列表（排除敏感字段）
    /// </summary>
    List<string> GetAccessibleFields(ClaimsPrincipal user, string entityName);
    
    /// <summary>
    /// 获取需要添加的数据范围过滤条件
    /// </summary>
    List<FilterCondition> GetDataScopeFilters(ClaimsPrincipal user, string entityName);
}

/// <summary>
/// 权限验证结果
/// </summary>
public class PermissionValidationResult
{
    /// <summary>是否验证通过</summary>
    public bool IsValid { get; set; }
    
    /// <summary>验证错误</summary>
    public List<ValidationError> Errors { get; set; } = [];
    
    /// <summary>需要添加的数据范围过滤条件</summary>
    public List<FilterCondition> RequiredFilters { get; set; } = [];
    
    /// <summary>需要从选择字段中移除的敏感字段</summary>
    public List<string> RemovedFields { get; set; } = [];
    
    /// <summary>权限警告信息</summary>
    public List<string> Warnings { get; set; } = [];
    
    /// <summary>创建成功结果</summary>
    public static PermissionValidationResult Ok() => new() { IsValid = true };
    
    /// <summary>创建失败结果</summary>
    public static PermissionValidationResult Fail(string errorCode, string message) => new()
    {
        IsValid = false,
        Errors = [new ValidationError { Code = errorCode, Message = message }]
    };
}
