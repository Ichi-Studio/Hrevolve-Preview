namespace Hrevolve.Agent.Services.Routing;

public interface IAgentSemanticRouter
{
    Task<RouteDecision> RouteAsync(
        string message,
        IReadOnlyList<AgentChatMessage> recentHistory,
        CancellationToken cancellationToken = default);
}

