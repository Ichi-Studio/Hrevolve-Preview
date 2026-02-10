using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Services.Metrics;
using OllamaSharp;

namespace Hrevolve.Agent.Services.Models;

public sealed class ConfiguredAgentChatClientProvider : IAgentChatClientProvider, IDisposable
{
    private readonly AiModelOptions _options;
    private readonly ILogger<ConfiguredAgentChatClientProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AgentMetrics _metrics;
    private readonly Lock _lock = new();
    private readonly Dictionary<ModelPurpose, (string ModelName, IChatClient Client)> _clients = new();

    public ConfiguredAgentChatClientProvider(
        IOptions<AiModelOptions> options,
        ILogger<ConfiguredAgentChatClientProvider> logger,
        ILoggerFactory loggerFactory,
        AgentMetrics metrics)
    {
        _options = options.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _metrics = metrics;
    }

    public IChatClient GetClient(ModelPurpose purpose)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(purpose, out var existing))
            {
                return existing.Client;
            }

            var created = CreateClient(purpose);
            _clients[purpose] = created;
            return created.Client;
        }
    }

    public string GetModelName(ModelPurpose purpose)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(purpose, out var existing))
            {
                return existing.ModelName;
            }

            var provider = (_options.Provider ?? "OpenAI").Trim();
            var modelName = ResolveModelName(purpose, provider);
            return modelName;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var client in _clients.Values.Select(v => v.Client))
            {
                client.Dispose();
            }
            _clients.Clear();
        }
    }

    private (string ModelName, IChatClient Client) CreateClient(ModelPurpose purpose)
    {
        var provider = (_options.Provider ?? "OpenAI").Trim();
        var primaryModelName = ResolveModelName(purpose, provider);

        var candidates = BuildCandidates(purpose, provider);
        var fallbackClient = new FallbackChatClient(
            candidates,
            _loggerFactory.CreateLogger<FallbackChatClient>(),
            _metrics,
            purpose,
            _options.TimeoutSeconds,
            _options.RetryCount,
            _options.RetryBackoffMs);

        _logger.LogInformation(
            "初始化模型客户端（provider={Provider}, purpose={Purpose}, primaryModel={Model}, candidates={CandidateCount}）",
            provider, purpose, primaryModelName, candidates.Count);

        return (primaryModelName, fallbackClient);
    }

    private IReadOnlyList<(string Provider, string Model, Func<IChatClient> Factory)> BuildCandidates(
        ModelPurpose purpose,
        string configuredProvider)
    {
        var provider = configuredProvider.Trim();
        var candidates = new List<(string Provider, string Model, Func<IChatClient> Factory)>();

        if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            var modelName = ResolveModelName(purpose, "ollama");
            candidates.Add(("ollama", modelName, () => CreateOllamaClient(modelName)));

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                var openAiModel = ResolveModelName(purpose, "openai");
                candidates.Add(("openai", openAiModel, () => CreateOpenAiClient(openAiModel)));
            }

            candidates.Add(("mock", "mock-model", () => new MockChatClient()));
            return candidates;
        }

        if (provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                var openAiModel = ResolveModelName(purpose, "openai");
                candidates.Add(("openai", openAiModel, () => CreateOpenAiClient(openAiModel)));
            }

            candidates.Add(("mock", "mock-model", () => new MockChatClient()));
            return candidates;
        }

        candidates.Add(("mock", "mock-model", () => new MockChatClient()));
        return candidates;
    }

    private IChatClient CreateOllamaClient(string modelName)
    {
        var endpoint = _options.Ollama.Endpoint;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            uri = new Uri("http://localhost:11434");
        }

        _logger.LogInformation("创建 Ollama 客户端（model={Model}） @ {Endpoint}", modelName, uri);
        return new OllamaApiClient(uri, modelName);
    }

    private IChatClient CreateOpenAiClient(string modelName)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI ApiKey is not configured.");
        }

        _logger.LogInformation("创建 OpenAI 客户端（model={Model}）", modelName);
        var client = new OpenAIClient(apiKey);
        return client.GetChatClient(modelName).AsIChatClient();
    }

    private string ResolveModelName(ModelPurpose purpose, string provider)
    {
        var model = _options.Model;
        var chatModel = _options.ChatModel;
        var text2SqlModel = _options.Text2SqlModel;
        var routerModel = _options.RouterModel;

        if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            chatModel = _options.Ollama.ChatModel ?? chatModel;
            text2SqlModel = _options.Ollama.Text2SqlModel ?? text2SqlModel;
            routerModel = _options.Ollama.RouterModel ?? routerModel;
        }

        return purpose switch
        {
            ModelPurpose.Chat => chatModel ?? model ?? "gpt-4o",
            ModelPurpose.Text2Sql => text2SqlModel ?? model ?? "gpt-4o",
            ModelPurpose.Router => routerModel ?? chatModel ?? model ?? "gpt-4o",
            _ => model ?? "gpt-4o"
        };
    }
}
