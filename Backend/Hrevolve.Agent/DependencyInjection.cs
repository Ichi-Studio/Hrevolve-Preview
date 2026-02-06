using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Services.Text2Sql;

namespace Hrevolve.Agent;

/// <summary>
/// Agent层依赖注入配置
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAgentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 配置AI Chat Client
        var aiConfig = configuration.GetSection("AI");
        var provider = aiConfig["Provider"] ?? "OpenAI";
        
        switch (provider.ToLowerInvariant())
        {
            case "openai":
                ConfigureOpenAI(services, aiConfig);
                break;
            case "azure":
                ConfigureAzureOpenAI(services, aiConfig);
                break;
            default:
                // 使用模拟客户端用于开发测试
                services.AddSingleton<IChatClient, MockChatClient>();
                break;
        }
        
        // 配置 Text2SQL 选项
        services.AddOptions<Text2SqlOptions>()
            .Bind(configuration.GetSection(Text2SqlOptions.SectionName));
        
        // 注册 Text2SQL 服务
        services.AddSingleton<ISchemaProvider, HrSchemaProvider>();
        services.AddScoped<ISqlSecurityValidator, SqlSecurityValidator>();
        services.AddScoped<IQueryPermissionValidator, QueryPermissionValidator>();
        services.AddScoped<IText2SqlService, Text2SqlService>();
        services.AddScoped<IQueryExecutor, DynamicQueryExecutor>();
        
        // 注册HR工具提供者（改为 Scoped 以支持注入 Scoped 服务）
        services.AddScoped<IHrToolProvider, HrToolProvider>();
        
        // 注册HR Agent服务
        services.AddScoped<IHrAgentService, HrAgentService>();
        
        return services;
    }
    
    private static void ConfigureOpenAI(IServiceCollection services, IConfigurationSection config)
    {
        var apiKey = config["ApiKey"];
        var model = config["Model"] ?? "gpt-4o";
        
        if (string.IsNullOrEmpty(apiKey))
        {
            // 没有配置API Key时使用模拟客户端
            services.AddSingleton<IChatClient, MockChatClient>();
            return;
        }
        
        services.AddSingleton<IChatClient>(sp =>
        {
            var client = new OpenAIClient(apiKey);
            return client.GetChatClient(model).AsIChatClient();
        });
    }
    
    private static void ConfigureAzureOpenAI(IServiceCollection services, IConfigurationSection config)
    {
        var endpoint = config["Endpoint"];
        var apiKey = config["ApiKey"];
        var deploymentName = config["DeploymentName"] ?? "gpt-4o";
        
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            services.AddSingleton<IChatClient, MockChatClient>();
            return;
        }
        
        // Azure OpenAI需要额外的包，暂时使用Mock
        // 生产环境请添加 Azure.AI.OpenAI 包并配置
        services.AddSingleton<IChatClient, MockChatClient>();
    }
}

/// <summary>
/// 模拟Chat Client - 用于开发测试
/// </summary>
public class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("MockChatClient", new Uri("http://localhost"), "mock-model");
    
    public void Dispose() { }
    
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken); // 模拟延迟
        
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";
        
        // 简单的模拟响应
        var response = lastUserMessage.ToLowerInvariant() switch
        {
            var s when s.Contains("假期") || s.Contains("年假") => 
                "您好！我来帮您查询假期余额。根据系统记录，您当前的年假余额为5天，病假余额为10天。如需请假，请告诉我具体的日期和原因。",
            var s when s.Contains("薪资") || s.Contains("工资") => 
                "您好！本月薪资已于15日发放，实发金额为15,300元。如需查看详细明细，请告诉我。",
            var s when s.Contains("考勤") || s.Contains("打卡") => 
                "您好！今日您已于09:02完成签到。如需查看历史考勤记录，请告诉我查询的时间范围。",
            var s when s.Contains("请假") => 
                "好的，我来帮您提交请假申请。请告诉我：1. 请假类型（年假/病假/事假）2. 开始日期 3. 结束日期 4. 请假原因",
            var s when s.Contains("组织") || s.Contains("部门") => 
                "我来为您查询组织架构信息。请问您想了解哪个部门的信息？",
            _ => "您好！我是Hrevolve HR助手，可以帮您查询假期余额、薪资信息、考勤记录，也可以协助您提交请假申请。请问有什么可以帮您的？"
        };
        
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
    }
    
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Mock client does not support streaming");
    }
    
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
