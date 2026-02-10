using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Services.Routing;
using Hrevolve.Agent.Services.Models;
using Hrevolve.Agent.Tests.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Tests;

public sealed class SemanticRouterTests
{
    [Fact]
    public async Task RouteAsync_HeuristicDataQuery_GoesToText2Sql()
    {
        var options = Options.Create(new AiModelOptions
        {
            Routing = new RoutingOptions
            {
                HeuristicRouteThreshold = 0.6,
                HeuristicIgnoreThreshold = 0.0
            }
        });

        var provider = new FakeAgentChatClientProvider(new()
        {
            [ModelPurpose.Router] = ("router", new FakeChatClient((_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"route\":\"chat\",\"confidence\":0.9}")))))
        });

        var router = new SemanticRouter(provider, options, NullLogger<SemanticRouter>.Instance);

        var decision = await router.RouteAsync("统计本月请假人数", [], CancellationToken.None);

        Assert.Equal(AgentRoute.Text2Sql, decision.Route);
        Assert.Equal("heuristic", decision.Strategy);
    }

    [Fact]
    public async Task RouteAsync_LowConfidenceHeuristic_UsesLlmJsonDecision()
    {
        var options = Options.Create(new AiModelOptions
        {
            Routing = new RoutingOptions
            {
                HeuristicRouteThreshold = 0.95,
                HeuristicIgnoreThreshold = 0.0
            }
        });

        var routerClient = new FakeChatClient((_, _, _) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "{\"route\":\"text2sql\",\"confidence\":0.9,\"reason\":\"data\"}"))));

        var provider = new FakeAgentChatClientProvider(new()
        {
            [ModelPurpose.Router] = ("router", routerClient)
        });

        var router = new SemanticRouter(provider, options, NullLogger<SemanticRouter>.Instance);

        var decision = await router.RouteAsync("查询一下考勤", [], CancellationToken.None);

        Assert.Equal(AgentRoute.Text2Sql, decision.Route);
        Assert.Equal("llm", decision.Strategy);
        Assert.True(decision.Confidence >= 0.6);
    }
}
