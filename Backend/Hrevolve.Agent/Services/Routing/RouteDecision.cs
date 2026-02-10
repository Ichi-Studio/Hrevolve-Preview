namespace Hrevolve.Agent.Services.Routing;

public sealed record RouteDecision(
    AgentRoute Route,
    double Confidence,
    string Strategy,
    string? Reason = null);

