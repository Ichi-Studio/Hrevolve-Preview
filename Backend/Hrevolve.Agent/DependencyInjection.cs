using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Services.Metrics;
using Hrevolve.Agent.Services.Text2Sql;
using Hrevolve.Agent.Services.Models;
using Hrevolve.Agent.Services.Routing;

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
        services.AddOptions<AiModelOptions>()
            .Bind(configuration.GetSection(AiModelOptions.SectionName));

        services.AddSingleton<IAgentChatClientProvider, ConfiguredAgentChatClientProvider>();
        services.AddSingleton<IAgentSemanticRouter, SemanticRouter>();
        services.AddSingleton<AgentMetrics>();
        
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
}
