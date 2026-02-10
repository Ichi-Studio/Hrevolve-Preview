namespace Hrevolve.Agent.Models;

public sealed record AgentChatEnvelope(
    string Reply,
    string Route,
    string CorrelationId,
    DateTimeOffset Timestamp,
    AgentDiagnostics? Diagnostics = null);
