using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// Text2SQL 转换服务接口 - 将自然语言转换为结构化查询
/// </summary>
public interface IText2SqlService
{
    /// <summary>
    /// 将自然语言查询转换为结构化查询请求
    /// </summary>
    /// <param name="naturalQuery">自然语言查询</param>
    /// <param name="conversationContext">可选对话上下文（用于消歧）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结构化查询请求</returns>
    Task<Text2SqlResult> ConvertAsync(
        string naturalQuery,
        string? conversationContext = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 验证查询意图是否属于 HR 领域
    /// </summary>
    /// <param name="query">查询文本</param>
    /// <returns>是否为有效的 HR 查询</returns>
    Task<bool> ValidateQueryIntentAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Text2SQL 转换结果
/// </summary>
public class Text2SqlResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>生成的查询请求</summary>
    public QueryRequest? QueryRequest { get; set; }
    
    /// <summary>LLM 原始响应（用于调试）</summary>
    public string? RawResponse { get; set; }
    
    /// <summary>处理耗时（毫秒）</summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>创建成功结果</summary>
    public static Text2SqlResult Ok(QueryRequest request) => new()
    {
        Success = true,
        QueryRequest = request
    };
    
    /// <summary>创建失败结果</summary>
    public static Text2SqlResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
