using System.Diagnostics;
using System.Text.Json;
using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Models.Text2Sql;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Services.Text2Sql;

/// <summary>
/// Text2SQL 转换服务实现 - 使用 LLM 将自然语言转换为结构化查询
/// </summary>
public class Text2SqlService : IText2SqlService
{
    private readonly IChatClient _chatClient;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ISqlSecurityValidator _securityValidator;
    private readonly Text2SqlOptions _options;
    private readonly ILogger<Text2SqlService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public Text2SqlService(
        IChatClient chatClient,
        ISchemaProvider schemaProvider,
        ISqlSecurityValidator securityValidator,
        IOptions<Text2SqlOptions> options,
        ILogger<Text2SqlService> logger)
    {
        _chatClient = chatClient;
        _schemaProvider = schemaProvider;
        _securityValidator = securityValidator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Text2SqlResult> ConvertAsync(string naturalQuery, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. 验证原始查询文本
            var textValidation = _securityValidator.ValidateRawText(naturalQuery);
            if (!textValidation.IsValid)
            {
                return Text2SqlResult.Fail(textValidation.Errors.First().Message);
            }

            // 2. 构建提示词
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(naturalQuery);

            // 3. 调用 LLM
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userPrompt)
            };

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var responseText = response.Text ?? "";

            _logger.LogDebug("LLM 响应: {Response}", responseText);

            // 4. 解析响应
            var queryRequest = ParseResponse(responseText, naturalQuery);
            if (queryRequest == null)
            {
                return new Text2SqlResult
                {
                    Success = false,
                    ErrorMessage = "无法理解您的查询，请尝试更具体的描述",
                    RawResponse = responseText,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 5. 验证生成的查询
            var validation = _securityValidator.Validate(queryRequest);
            if (!validation.IsValid)
            {
                return new Text2SqlResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors.Select(e => e.Message)),
                    RawResponse = responseText,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 使用可能被修正的请求
            queryRequest = validation.CorrectedRequest ?? queryRequest;

            stopwatch.Stop();
            return new Text2SqlResult
            {
                Success = true,
                QueryRequest = queryRequest,
                RawResponse = responseText,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text2SQL 转换失败: {Query}", naturalQuery);
            return new Text2SqlResult
            {
                Success = false,
                ErrorMessage = "查询转换过程中发生错误，请稍后重试",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<bool> ValidateQueryIntentAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        // 简单的关键词检测
        var hrKeywords = new[]
        {
            "员工", "考勤", "打卡", "请假", "假期", "薪资", "工资", "部门", "组织",
            "入职", "离职", "迟到", "早退", "加班", "休假", "年假", "病假",
            "employee", "attendance", "leave", "salary", "department", "organization"
        };

        var lowerQuery = query.ToLowerInvariant();
        return hrKeywords.Any(keyword => lowerQuery.Contains(keyword.ToLowerInvariant()));
    }

    private string BuildSystemPrompt()
    {
        var schemaDescription = _schemaProvider.GetPromptSchemaDescription();
        var examples = BuildExamplesPrompt();

        return $"""
            你是 Hrevolve HR 系统的数据查询助手。你的任务是将用户的自然语言查询转换为结构化的 JSON 查询对象。

            ## 规则
            1. 只能生成针对 HR 数据的查询，包括员工、考勤、请假、薪资、部门等
            2. 必须返回有效的 JSON 格式
            3. 不要生成任何解释文字，只返回 JSON
            4. 对于模糊的查询，选择最合理的解释
            5. 日期参数使用特殊标记：@Today, @CurrentWeekStart, @CurrentMonthStart, @CurrentYear
            6. 默认返回 100 条记录，最多 {_options.MaxResultRows} 条

            {schemaDescription}

            ## 输出格式
            返回一个 JSON 对象，包含以下字段：
            - operation: "Select" | "Insert" | "Update" | "Delete"
            - targetEntity: 目标实体名称（如 "Employee", "AttendanceRecord"）
            - selectFields: 要查询的字段列表（数组）
            - filters: 过滤条件列表，每个条件包含 field, operator, value
            - joins: JOIN 子句列表，每个包含 entity, on
            - aggregation: 聚合类型（可选）: "Count", "Sum", "Avg", "Min", "Max", "CountDistinct"
            - aggregationField: 聚合字段（当有聚合时必填）
            - groupByFields: 分组字段列表（可选）
            - orderBy: 排序列表，每个包含 field, descending
            - limit: 返回行数限制
            - updateValues: 更新/插入的值（用于 Insert/Update 操作）

            ## 过滤条件操作符
            - Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual
            - Contains, StartsWith, EndsWith
            - In, NotIn, IsNull, IsNotNull, Between

            {examples}
            """;
    }

    private string BuildExamplesPrompt()
    {
        var examples = _schemaProvider.GetQueryExamples();
        if (examples.Count == 0)
        {
            return "";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## 示例");
        sb.AppendLine();

        foreach (var example in examples.Take(5)) // 只取前 5 个示例
        {
            sb.AppendLine($"输入: \"{example.Input}\"");
            sb.AppendLine($"输出: {JsonSerializer.Serialize(example.ExpectedOutput, JsonOptions)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildUserPrompt(string naturalQuery)
    {
        return $"""
            请将以下查询转换为 JSON 格式的结构化查询：

            {naturalQuery}

            只返回 JSON，不要任何其他文字。
            """;
    }

    private QueryRequest? ParseResponse(string responseText, string originalQuery)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            // 尝试提取 JSON 部分（LLM 可能返回额外的文字）
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("响应中未找到有效的 JSON: {Response}", responseText);
                return null;
            }

            var jsonText = responseText[jsonStart..(jsonEnd + 1)];
            var request = JsonSerializer.Deserialize<QueryRequest>(jsonText, JsonOptions);

            if (request != null)
            {
                request.OriginalQuery = originalQuery;

                // 应用默认值
                if (request.Limit <= 0)
                {
                    request.Limit = _options.DefaultResultRows;
                }

                // 处理日期占位符
                ProcessDatePlaceholders(request);
            }

            return request;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON 解析失败: {Response}", responseText);
            return null;
        }
    }

    private static void ProcessDatePlaceholders(QueryRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        foreach (var filter in request.Filters)
        {
            if (filter.Value is string strValue)
            {
                filter.Value = strValue switch
                {
                    "@Today" => today,
                    "@CurrentWeekStart" => today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday),
                    "@CurrentMonthStart" => new DateOnly(today.Year, today.Month, 1),
                    "@CurrentYear" => today.Year,
                    "@Now" => now,
                    _ => filter.Value
                };
            }
        }
    }
}
