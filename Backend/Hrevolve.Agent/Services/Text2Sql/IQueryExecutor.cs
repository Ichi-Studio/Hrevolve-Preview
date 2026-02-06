using System.Security.Claims;
using Hrevolve.Agent.Models.Text2Sql;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// 查询执行器接口 - 执行结构化查询
/// </summary>
public interface IQueryExecutor
{
    /// <summary>
    /// 执行查询
    /// </summary>
    /// <param name="request">结构化查询请求</param>
    /// <param name="user">当前用户（用于权限过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    Task<QueryResult> ExecuteAsync(QueryRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
