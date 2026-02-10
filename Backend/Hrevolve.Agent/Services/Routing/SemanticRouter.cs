using System.Text.Json;
using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Services.Models;

namespace Hrevolve.Agent.Services.Routing;

public sealed class SemanticRouter : IAgentSemanticRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAgentChatClientProvider _clientProvider;
    private readonly RoutingOptions _routingOptions;
    private readonly ILogger<SemanticRouter> _logger;

    public SemanticRouter(
        IAgentChatClientProvider clientProvider,
        IOptions<AiModelOptions> options,
        ILogger<SemanticRouter> logger)
    {
        _clientProvider = clientProvider;
        _routingOptions = options.Value.Routing;
        _logger = logger;
    }

    public async Task<RouteDecision> RouteAsync(
        string message,
        IReadOnlyList<AgentChatMessage> recentHistory,
        CancellationToken cancellationToken = default)
    {
        var (dataScore, chatScore, reason) = ScoreHeuristic(message);

        if (dataScore >= _routingOptions.HeuristicRouteThreshold && dataScore >= chatScore + 0.15)
        {
            return new RouteDecision(AgentRoute.Text2Sql, dataScore, "heuristic", reason);
        }

        if (chatScore >= _routingOptions.HeuristicRouteThreshold && chatScore >= dataScore + 0.15)
        {
            return new RouteDecision(AgentRoute.Chat, chatScore, "heuristic", reason);
        }

        if (Math.Max(dataScore, chatScore) <= _routingOptions.HeuristicIgnoreThreshold)
        {
            return new RouteDecision(AgentRoute.Chat, 0.5, "heuristic", "low-signal");
        }

        var llmDecision = await TryRouteWithLlmAsync(message, recentHistory, cancellationToken);
        if (llmDecision is not null)
        {
            return llmDecision;
        }

        var fallbackRoute = dataScore >= chatScore ? AgentRoute.Text2Sql : AgentRoute.Chat;
        var fallbackConfidence = Math.Max(dataScore, chatScore);
        return new RouteDecision(fallbackRoute, fallbackConfidence, "fallback", "llm-route-failed");
    }

    private async Task<RouteDecision?> TryRouteWithLlmAsync(
        string message,
        IReadOnlyList<AgentChatMessage> recentHistory,
        CancellationToken cancellationToken)
    {
        try
        {
            var routerClient = _clientProvider.GetClient(ModelPurpose.Router);
            var routerModel = _clientProvider.GetModelName(ModelPurpose.Router);

            var context = BuildContextSnippet(recentHistory);
            var system = """
                你是一个意图路由器。你的任务是判断用户输入应当路由到：
                - text2sql：当用户要查询/统计 HR 系统数据（员工、考勤、请假、薪资、部门等），需要生成/执行查询
                - chat：当用户在闲聊、问候、解释性问题、政策咨询、流程说明、建议类问题

                只输出 JSON，不要任何额外文字。JSON 格式如下：
                {"route":"text2sql|chat","confidence":0.0,"reason":"一句话原因"}
                """;

            var user = $"""
                对话上下文（用于消歧，可为空）：
                {context}

                用户输入：
                {message}
                """;

            var response = await routerClient.GetResponseAsync(
                [new(ChatRole.System, system), new(ChatRole.User, user)],
                cancellationToken: cancellationToken);

            var text = response.Text ?? string.Empty;
            var json = ExtractJsonObject(text);
            if (json is null)
            {
                _logger.LogWarning("路由 LLM 未返回有效 JSON（model={Model}）: {Text}", routerModel, text);
                return null;
            }

            var parsed = JsonSerializer.Deserialize<RouteJson>(json, JsonOptions);
            if (parsed is null)
            {
                _logger.LogWarning("路由 LLM JSON 解析失败（model={Model}）: {Json}", routerModel, json);
                return null;
            }

            var route = parsed.Route?.Trim().ToLowerInvariant() switch
            {
                "text2sql" => AgentRoute.Text2Sql,
                "chat" => AgentRoute.Chat,
                _ => (AgentRoute?)null
            };

            if (route is null)
            {
                _logger.LogWarning("路由 LLM 返回未知 route（model={Model}）: {Json}", routerModel, json);
                return null;
            }

            var confidence = Math.Clamp(parsed.Confidence, 0, 1);
            if (confidence < 0.6)
            {
                return null;
            }

            return new RouteDecision(route.Value, confidence, "llm", parsed.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "路由 LLM 调用失败");
            return null;
        }
    }

    private static string BuildContextSnippet(IReadOnlyList<AgentChatMessage> recentHistory)
    {
        if (recentHistory.Count == 0)
        {
            return "(空)";
        }

        var lines = recentHistory
            .TakeLast(6)
            .Select(m => $"{m.Role}: {m.Content}");

        return string.Join("\n", lines);
    }

    private static (double DataScore, double ChatScore, string Reason) ScoreHeuristic(string message)
    {
        var input = message.Trim();
        if (input.Length == 0)
        {
            return (0, 1, "empty");
        }

        var lower = input.ToLowerInvariant();

        var dataKeywords = new[]
        {
            "查询", "统计", "筛选", "列出", "展示", "多少", "总数", "人数", "数量", "平均", "最大", "最小", "top", "排名",
            "本月", "上月", "本周", "上周", "今年", "去年", "最近", "截止", "从", "到",
            "员工", "考勤", "打卡", "请假", "假期", "薪资", "工资", "部门", "组织", "入职", "离职", "加班", "迟到", "早退",
            "employee", "attendance", "leave", "salary", "department", "organization", "count", "sum", "avg"
        };

        var chatKeywords = new[]
        {
            "你好", "您好", "在吗", "你是谁", "你能做什么", "帮我", "谢谢", "再见",
            "怎么", "为什么", "解释", "介绍", "建议", "推荐", "流程", "规定", "政策", "制度", "规则", "假期政策", "报销政策",
            "聊天", "闲聊", "讲个", "笑话"
        };

        var dataHits = dataKeywords.Count(k => lower.Contains(k));
        var chatHits = chatKeywords.Count(k => lower.Contains(k));

        var hasQuestionMark = input.Contains('？') || input.Contains('?');
        var hasNumberOrDate = input.Any(char.IsDigit) || lower.Contains("yyyy") || lower.Contains("202");

        var dataScore = Math.Clamp((dataHits / 6.0) + (hasNumberOrDate ? 0.15 : 0) + (hasQuestionMark ? 0.05 : 0), 0, 1);
        var chatScore = Math.Clamp((chatHits / 5.0) + (hasQuestionMark ? 0.1 : 0), 0, 1);

        var reason = $"dataHits={dataHits},chatHits={chatHits}";
        return (dataScore, chatScore, reason);
    }

    private static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }

    private sealed class RouteJson
    {
        public string? Route { get; set; }

        public double Confidence { get; set; }

        public string? Reason { get; set; }
    }
}

