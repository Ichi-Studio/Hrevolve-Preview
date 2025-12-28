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
    Task<string> ChatAsync(Guid employeeId, string message, CancellationToken cancellationToken = default);
    
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
public class HrAgentService(
    IChatClient chatClient,
    IHrToolProvider toolProvider,
    ILogger<HrAgentService> logger) : IHrAgentService
{
    // 对话历史存储（生产环境应使用Redis或数据库）
    private static readonly Dictionary<Guid, List<ChatMessage>> _chatHistories = new();
    private static readonly object _lock = new();
    
    public async Task<string> ChatAsync(Guid employeeId, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("处理员工 {EmployeeId} 的消息", employeeId);
        
        // 获取或创建对话历史
        var chatHistory = GetOrCreateChatHistory(employeeId);
        
        // 添加用户消息
        chatHistory.Add(new ChatMessage(ChatRole.User, message));
        
        try
        {
            // 构建聊天选项，包含工具
            var options = new ChatOptions
            {
                Tools = toolProvider.GetTools()
            };
            
            // 调用AI模型
            var response = await chatClient.GetResponseAsync(chatHistory.AsEnumerable(), options, cancellationToken);
            
            var assistantMessage = response.Text ?? "抱歉，我无法处理您的请求。";
            
            // 添加助手回复到历史
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));
            
            // 限制历史长度
            TrimChatHistory(chatHistory, maxMessages: 20);
            
            return assistantMessage;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HR Agent处理消息失败");
            return "抱歉，系统暂时无法处理您的请求，请稍后重试。";
        }
    }
    
    public Task<IReadOnlyList<AgentChatMessage>> GetChatHistoryAsync(Guid employeeId, int limit = 20)
    {
        lock (_lock)
        {
            if (_chatHistories.TryGetValue(employeeId, out var history))
            {
                var messages = history
                    .Skip(1) // 跳过系统消息
                    .TakeLast(limit)
                    .Select(m => new AgentChatMessage(m.Role.Value, m.Text ?? ""))
                    .ToList();
                
                return Task.FromResult<IReadOnlyList<AgentChatMessage>>(messages);
            }
        }
        
        return Task.FromResult<IReadOnlyList<AgentChatMessage>>(Array.Empty<AgentChatMessage>());
    }
    
    public Task ClearChatHistoryAsync(Guid employeeId)
    {
        lock (_lock)
        {
            _chatHistories.Remove(employeeId);
        }
        return Task.CompletedTask;
    }
    
    private List<ChatMessage> GetOrCreateChatHistory(Guid employeeId)
    {
        lock (_lock)
        {
            if (!_chatHistories.TryGetValue(employeeId, out var history))
            {
                history = [new ChatMessage(ChatRole.System, GetSystemPrompt())];
                _chatHistories[employeeId] = history;
            }
            return history;
        }
    }
    
    private static string GetSystemPrompt()
    {
        return """
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
    }
    
    private static void TrimChatHistory(List<ChatMessage> history, int maxMessages)
    {
        // 保留系统消息和最近的消息
        while (history.Count > maxMessages + 1)
        {
            history.RemoveAt(1); // 移除最早的非系统消息
        }
    }
}

/// <summary>
/// Agent聊天消息DTO
/// </summary>
public record AgentChatMessage(string Role, string Content);
