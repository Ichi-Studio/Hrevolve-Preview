using System.Collections.Concurrent;
using System.Diagnostics;
using Hrevolve.Agent.Services.Metrics;

namespace Hrevolve.Agent.Services.Models;

public sealed class FallbackChatClient : IChatClient
{
    private readonly IReadOnlyList<(string Provider, string Model, Func<IChatClient> Factory)> _candidates;
    private readonly ILogger<FallbackChatClient> _logger;
    private readonly AgentMetrics _metrics;
    private readonly ModelPurpose _purpose;
    private readonly int _timeoutSeconds;
    private readonly int _retryCount;
    private readonly int _retryBackoffMs;

    private readonly ConcurrentDictionary<int, IChatClient> _instances = new();

    public FallbackChatClient(
        IReadOnlyList<(string Provider, string Model, Func<IChatClient> Factory)> candidates,
        ILogger<FallbackChatClient> logger,
        AgentMetrics metrics,
        ModelPurpose purpose,
        int timeoutSeconds,
        int retryCount,
        int retryBackoffMs)
    {
        _candidates = candidates;
        _logger = logger;
        _metrics = metrics;
        _purpose = purpose;
        _timeoutSeconds = timeoutSeconds <= 0 ? 60 : timeoutSeconds;
        _retryCount = Math.Max(0, retryCount);
        _retryBackoffMs = retryBackoffMs <= 0 ? 250 : retryBackoffMs;
    }

    public void Dispose()
    {
        foreach (var client in _instances.Values)
        {
            client.Dispose();
        }
        _instances.Clear();
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_candidates.Count == 0)
        {
            throw new InvalidOperationException("No chat client candidates configured.");
        }

        Exception? lastException = null;

        for (var i = 0; i < _candidates.Count; i++)
        {
            var (provider, model, _) = _candidates[i];

            for (var attempt = 0; attempt < 1 + _retryCount; attempt++)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var client = GetOrCreateClient(i);
                    var response = await client.GetResponseAsync(chatMessages, options, linked.Token);
                    sw.Stop();
                    _metrics.RecordModelLatency(_purpose, sw.Elapsed.TotalMilliseconds, provider, model);
                    if (i > 0 || attempt > 0)
                    {
                        _logger.LogInformation(
                            "模型调用已恢复（provider={Provider}, model={Model}, attempt={Attempt}, fallbackIndex={Index})",
                            provider, model, attempt + 1, i);
                    }
                    if (i > 0)
                    {
                        _metrics.RecordFallback(_candidates[0].Provider, provider, "exception");
                    }
                    return response;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new TimeoutException($"Model call timeout after {_timeoutSeconds}s (provider={provider}, model={model}).");
                    sw.Stop();
                    _metrics.RecordModelError(_purpose, provider);
                    _metrics.RecordModelLatency(_purpose, sw.Elapsed.TotalMilliseconds, provider, model);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    sw.Stop();
                    _metrics.RecordModelError(_purpose, provider);
                    _metrics.RecordModelLatency(_purpose, sw.Elapsed.TotalMilliseconds, provider, model);
                }

                _logger.LogWarning(
                    lastException,
                    "模型调用失败（provider={Provider}, model={Model}, attempt={Attempt}/{MaxAttempt}, fallbackIndex={Index}）",
                    provider, model, attempt + 1, 1 + _retryCount, i);

                if (attempt < _retryCount)
                {
                    var backoff = _retryBackoffMs * (attempt + 1);
                    await Task.Delay(backoff, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException("All chat client candidates failed.", lastException);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Fallback client does not support streaming.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private IChatClient GetOrCreateClient(int index)
    {
        return _instances.GetOrAdd(index, i =>
        {
            var (_, _, factory) = _candidates[i];
            return factory();
        });
    }
}
