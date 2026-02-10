using System.Security.Claims;
using System.Diagnostics;
using Hrevolve.Agent.Services.Models;
using Hrevolve.Agent.Services.Routing;
using Hrevolve.Agent.Models;
using Hrevolve.Agent.Services.Metrics;

namespace Hrevolve.Agent.Services;

/// <summary>
/// HR Agent服务 - 基于Microsoft Agent Framework的AI助手
/// 使用Microsoft.Extensions.AI统一抽象层
/// </summary>
public interface IHrAgentService
{
    /// <summary>
    /// 处理用户消息
    /// </summary>
    Task<AgentChatEnvelope> ChatAsync(Guid employeeId, string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取对话历史
    /// </summary>
    Task<IReadOnlyList<AgentChatMessage>> GetChatHistoryAsync(Guid employeeId, int limit = 20);
    
    /// <summary>
    /// 清除对话历史
    /// </summary>
    Task ClearChatHistoryAsync(Guid employeeId);
}

/// <summary>
/// HR Agent服务实现
/// </summary>
public sealed class HrAgentService : IHrAgentService
{
    private readonly IAgentChatClientProvider _clientProvider;
    private readonly IHrToolProvider _toolProvider;
    private readonly IAgentSemanticRouter _router;
    private readonly IText2SqlService _text2SqlService;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly Text2SqlOptions _text2SqlOptions;
    private readonly AgentMetrics _metrics;
    private readonly ILogger<HrAgentService> _logger;

    // 对话历史存储（生产环境应使用Redis或数据库）
    private static readonly Dictionary<Guid, List<AgentChatHistoryMessage>> _chatHistories = [];
    private static readonly Lock _lock = new();
    private const int DefaultChatHistoryLimit = 20;

    public HrAgentService(
        IAgentChatClientProvider clientProvider,
        IHrToolProvider toolProvider,
        IAgentSemanticRouter router,
        IText2SqlService text2SqlService,
        IQueryExecutor queryExecutor,
        ICurrentUserAccessor currentUserAccessor,
        IOptions<Text2SqlOptions> text2SqlOptions,
        AgentMetrics metrics,
        ILogger<HrAgentService> logger)
    {
        _clientProvider = clientProvider;
        _toolProvider = toolProvider;
        _router = router;
        _text2SqlService = text2SqlService;
        _queryExecutor = queryExecutor;
        _currentUserAccessor = currentUserAccessor;
        _text2SqlOptions = text2SqlOptions.Value;
        _metrics = metrics;
        _logger = logger;
    }
    
    public async Task<AgentChatEnvelope> ChatAsync(Guid employeeId, string message, CancellationToken cancellationToken = default)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        _logger.LogInformation("处理员工 {EmployeeId} 的消息", employeeId);
        
        // 获取或创建对话历史
        var chatHistory = GetOrCreateChatHistory(employeeId);
        
        // 添加用户消息
        chatHistory.Add(new AgentChatHistoryMessage(ChatRole.User, message, DateTime.UtcNow));
        
        try
        {
            var recentHistory = chatHistory
                .Skip(1)
                .TakeLast(8)
                .Select(m => new AgentChatMessage(m.Role.Value, m.Content, m.TimestampUtc.ToString("o")))
                .ToList();

            var routeDecision = await _router.RouteAsync(message, recentHistory, cancellationToken);
            _metrics.RecordRoute(routeDecision.Route, routeDecision.Strategy);

            using var _ = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["employeeId"] = employeeId,
                ["route"] = routeDecision.Route.ToString(),
                ["routeStrategy"] = routeDecision.Strategy,
                ["routeConfidence"] = routeDecision.Confidence
            });

            if (routeDecision.Route == AgentRoute.Text2Sql)
            {
                var (routeUsed, reply, diagnostics) = await HandleText2SqlAsync(message, chatHistory, recentHistory, cancellationToken);
                TrimChatHistory(chatHistory, maxMessages: DefaultChatHistoryLimit);
                return new AgentChatEnvelope(
                    Reply: reply,
                    Route: routeUsed == AgentRoute.Text2Sql ? "text2sql" : "chat",
                    CorrelationId: correlationId,
                    Timestamp: DateTimeOffset.UtcNow,
                    Diagnostics: diagnostics);
            }

            var assistantMessage = await HandleChatAsync(chatHistory, cancellationToken);
            TrimChatHistory(chatHistory, maxMessages: DefaultChatHistoryLimit);
            return new AgentChatEnvelope(
                Reply: assistantMessage,
                Route: "chat",
                CorrelationId: correlationId,
                Timestamp: DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("HR Agent请求已取消");
            return new AgentChatEnvelope(
                Reply: "请求已取消。",
                Route: "chat",
                CorrelationId: correlationId,
                Timestamp: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HR Agent处理消息失败");
            return new AgentChatEnvelope(
                Reply: "抱歉，系统暂时无法处理您的请求，请稍后重试。",
                Route: "chat",
                CorrelationId: correlationId,
                Timestamp: DateTimeOffset.UtcNow);
        }
    }

    private async Task<string> HandleChatAsync(
        List<AgentChatHistoryMessage> chatHistory,
        CancellationToken cancellationToken)
    {
        var options = new ChatOptions
        {
            Tools = _toolProvider.GetTools()
        };

        var chatClient = _clientProvider.GetClient(ModelPurpose.Chat);
        var response = await chatClient.GetResponseAsync(chatHistory.Select(m => m.ToChatMessage()), options, cancellationToken);
        var assistantMessage = response.Text ?? "抱歉，我无法处理您的请求。";

        chatHistory.Add(new AgentChatHistoryMessage(ChatRole.Assistant, assistantMessage, DateTime.UtcNow));
        return assistantMessage;
    }

    private async Task<(AgentRoute RouteUsed, string Reply, AgentDiagnostics? Diagnostics)> HandleText2SqlAsync(
        string message,
        List<AgentChatHistoryMessage> chatHistory,
        IReadOnlyList<AgentChatMessage> recentHistory,
        CancellationToken cancellationToken)
    {
        var context = recentHistory.Count == 0
            ? null
            : string.Join("\n", recentHistory.TakeLast(6).Select(m => $"{m.Role}: {m.Content}"));

        var conversion = await _text2SqlService.ConvertAsync(message, context, cancellationToken);
        if (!conversion.Success || conversion.QueryRequest == null)
        {
            var reply = await AskForClarificationWithChatAsync(message, conversion.ErrorMessage, chatHistory, cancellationToken);
            return (AgentRoute.Chat, reply, new AgentDiagnostics(Warnings: ["text2sql-convert-failed"]));
        }

        var claimsPrincipal = CurrentUserClaimsPrincipalFactory.Create(_currentUserAccessor.CurrentUser);
        var result = await _queryExecutor.ExecuteAsync(conversion.QueryRequest, claimsPrincipal, cancellationToken);

        if (!result.Success)
        {
            var reply = await AskForClarificationWithChatAsync(message, result.ErrorMessage, chatHistory, cancellationToken);
            return (AgentRoute.Chat, reply, new AgentDiagnostics(Warnings: ["text2sql-execute-failed"]));
        }

        var summary = SummarizeQueryResult(result, conversion.QueryRequest);
        chatHistory.Add(new AgentChatHistoryMessage(ChatRole.Assistant, summary, DateTime.UtcNow));
        var diagnostics = new AgentDiagnostics(
            GeneratedSql: _text2SqlOptions.IncludeGeneratedSqlInResponse ? result.GeneratedSql : null,
            ExecutionMs: result.ExecutionTimeMs,
            Warnings: result.Warnings.Count == 0 ? null : result.Warnings);
        return (AgentRoute.Text2Sql, summary, diagnostics);
    }

    private async Task<string> AskForClarificationWithChatAsync(
        string originalMessage,
        string? failureReason,
        List<AgentChatHistoryMessage> chatHistory,
        CancellationToken cancellationToken)
    {
        var chatClient = _clientProvider.GetClient(ModelPurpose.Chat);
        var system = """
            你是 Hrevolve HR 助手。用户的输入更像是在做数据查询，但当前系统无法安全地直接执行。
            你的目标是提出1-2个澄清问题，帮助用户把查询说清楚（例如时间范围、部门、人员范围、指标口径）。
            回复要简短、具体，不要提及内部实现细节。
            """;

        var user = $"""
            用户输入：
            {originalMessage}

            失败原因（可为空）：
            {failureReason ?? "(空)"}
            """;

        var response = await chatClient.GetResponseAsync([new(ChatRole.System, system), new(ChatRole.User, user)], cancellationToken: cancellationToken);
        var assistantMessage = response.Text ?? "为了更准确地查询，请补充一下您想查询的时间范围和对象范围。";

        chatHistory.Add(new AgentChatHistoryMessage(ChatRole.Assistant, assistantMessage, DateTime.UtcNow));
        return assistantMessage;
    }

    private static string SummarizeQueryResult(QueryResult result, QueryRequest request)
    {
        if (result.AggregationResult != null)
        {
            var aggregationName = request.Aggregation switch
            {
                AggregationType.Count => "数量",
                AggregationType.CountDistinct => "去重数量",
                AggregationType.Sum => "总和",
                AggregationType.Avg => "平均值",
                AggregationType.Min => "最小值",
                AggregationType.Max => "最大值",
                _ => "结果"
            };
            return $"查询结果 - {aggregationName}: {result.AggregationResult}";
        }

        if (result.Operation != QueryOperation.Select)
        {
            var operationName = result.Operation switch
            {
                QueryOperation.Insert => "新增",
                QueryOperation.Update => "更新",
                QueryOperation.Delete => "删除",
                _ => "操作"
            };

            var lines = new List<string> { $"{operationName}成功，影响 {result.AffectedRows} 条记录" };
            if (result.InsertedId.HasValue)
            {
                lines.Add($"新记录ID: {result.InsertedId}");
            }
            return string.Join("\n", lines);
        }

        if (result.Data.Count == 0)
        {
            return "未找到符合条件的数据";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"查询结果（共 {result.RowCount} 条记录）：");
        sb.AppendLine();

        if (result.Columns.Count > 0)
        {
            var headers = result.Columns.Select(c => c.DisplayName ?? c.Name);
            sb.AppendLine(string.Join(" | ", headers));
            sb.AppendLine(new string('-', Math.Min(80, headers.Sum(h => h.Length + 3))));
        }

        var displayCount = Math.Min(result.Data.Count, 20);
        for (var i = 0; i < displayCount; i++)
        {
            var row = result.Data[i];
            var values = result.Columns.Count > 0
                ? result.Columns.Select(c => FormatValue(row.GetValueOrDefault(c.Name)))
                : row.Values.Select(FormatValue);
            sb.AppendLine(string.Join(" | ", values));
        }

        if (result.Data.Count > displayCount)
        {
            sb.AppendLine($"... 还有 {result.Data.Count - displayCount} 条记录未显示");
        }

        sb.AppendLine();
        sb.AppendLine($"查询耗时: {result.ExecutionTimeMs}ms");

        return sb.ToString();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "-",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
            DateOnly date => date.ToString("yyyy-MM-dd"),
            TimeOnly time => time.ToString("HH:mm"),
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            bool b => b ? "是" : "否",
            Enum e => e.ToString(),
            _ => value.ToString() ?? "-"
        };
    }
    
    public Task<IReadOnlyList<AgentChatMessage>> GetChatHistoryAsync(Guid employeeId, int limit = DefaultChatHistoryLimit)
    {
        lock (_lock)
        {
            if (_chatHistories.TryGetValue(employeeId, out var history))
            {
                var messages = history
                    .Skip(1) // 跳过系统消息
                    .TakeLast(limit)
                    .Select(m => new AgentChatMessage(m.Role.Value, m.Content, m.TimestampUtc.ToString("o")))
                    .ToList();

                return Task.FromResult<IReadOnlyList<AgentChatMessage>>(messages);
            }
        }

        return Task.FromResult<IReadOnlyList<AgentChatMessage>>([]);
    }
    
    public Task ClearChatHistoryAsync(Guid employeeId)
    {
        lock (_lock)
        {
            _chatHistories.Remove(employeeId);
        }
        return Task.CompletedTask;
    }
    
    private static List<AgentChatHistoryMessage> GetOrCreateChatHistory(Guid employeeId)
    {
        lock (_lock)
        {
            if (!_chatHistories.TryGetValue(employeeId, out var history))
            {
                history = [new AgentChatHistoryMessage(ChatRole.System, GetSystemPrompt, DateTime.UtcNow)];
                _chatHistories[employeeId] = history;
            }
            return history;
        }
    }
    
    private const string GetSystemPrompt = 
           """
            你是Hrevolve HR助手，一个专业、友好的人力资源AI助手。
            
            你的职责包括：
            1. 回答员工关于公司政策、规章制度的问题
            2. 帮助员工查询假期余额、薪资信息、考勤记录
            3. 协助员工提交请假申请、报销申请等
            4. 提供组织架构、同事联系方式等信息查询
            
            注意事项：
            - 始终保持专业、友好的态度
            - 涉及敏感信息（如薪资）时，只能查询员工本人的信息
            - 如果不确定答案，请诚实告知并建议联系HR部门
            - 使用简洁清晰的中文回复
            - 如果需要执行操作（如请假），请先确认所有必要信息
            - 当需要调用工具时，请使用提供的工具函数
            """;
    
    
    private static void TrimChatHistory(List<AgentChatHistoryMessage> history, int maxMessages)
    {
        // 保留系统消息和最近的消息
        while (history.Count > maxMessages + 1)
        {
            history.RemoveAt(1); // 移除最早的非系统消息
        }
    }
}

internal record AgentChatHistoryMessage(ChatRole Role, string Content, DateTime TimestampUtc)
{
    public ChatMessage ToChatMessage() => new(Role, Content);
}

/// <summary>
/// Agent聊天消息DTO
/// </summary>
public record AgentChatMessage(string Role, string Content, string Timestamp);

internal static class CurrentUserClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(ICurrentUser? currentUser)
    {
        if (currentUser == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>();

        if (currentUser.Id.HasValue)
        {
            claims.Add(new Claim("user_id", currentUser.Id.Value.ToString()));
        }
        if (currentUser.EmployeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", currentUser.EmployeeId.Value.ToString()));
        }
        if (currentUser.TenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", currentUser.TenantId.Value.ToString()));
        }

        foreach (var role in currentUser.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in currentUser.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }
}
