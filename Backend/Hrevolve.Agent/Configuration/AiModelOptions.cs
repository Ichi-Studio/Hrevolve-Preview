namespace Hrevolve.Agent.Configuration;

public sealed class AiModelOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenAI";

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public string? ChatModel { get; set; }

    public string? Text2SqlModel { get; set; }

    public string? RouterModel { get; set; }

    public string? Endpoint { get; set; }

    public string? DeploymentName { get; set; }

    public OllamaOptions Ollama { get; set; } = new();

    public RoutingOptions Routing { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 60;

    public int RetryCount { get; set; } = 1;

    public int RetryBackoffMs { get; set; } = 250;
}

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";

    public string? ChatModel { get; set; }

    public string? Text2SqlModel { get; set; }

    public string? RouterModel { get; set; }
}

public sealed class RoutingOptions
{
    public double HeuristicRouteThreshold { get; set; } = 0.75;

    public double HeuristicIgnoreThreshold { get; set; } = 0.25;

    public int ContextMessagesForDisambiguation { get; set; } = 6;
}
