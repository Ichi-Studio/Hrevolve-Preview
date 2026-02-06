using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// SQL 安全验证器接口 - 验证查询请求的安全性
/// </summary>
public interface ISqlSecurityValidator
{
    /// <summary>
    /// 验证查询请求的安全性
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>验证结果</returns>
    ValidationResult Validate(QueryRequest request);
    
    /// <summary>
    /// 验证原始查询文本中是否包含危险关键字
    /// </summary>
    /// <param name="queryText">查询文本</param>
    /// <returns>验证结果</returns>
    ValidationResult ValidateRawText(string queryText);
    
    /// <summary>
    /// 计算查询复杂度分数
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>复杂度分数</returns>
    int CalculateComplexityScore(QueryRequest request);
}
