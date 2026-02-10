namespace Hrevolve.Agent.Models;

public sealed record AgentDiagnostics(
    string? GeneratedSql = null,
    long? ExecutionMs = null,
    IReadOnlyList<string>? Warnings = null);
