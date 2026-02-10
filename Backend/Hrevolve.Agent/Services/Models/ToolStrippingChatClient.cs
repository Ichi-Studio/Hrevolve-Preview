namespace Hrevolve.Agent.Services.Models;

public sealed class ToolStrippingChatClient : IChatClient
{
    private readonly IChatClient _inner;

    public ToolStrippingChatClient(IChatClient inner)
    {
        _inner = inner;
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options?.Tools is { Count: > 0 })
        {
            return _inner.GetResponseAsync(chatMessages, new ChatOptions(), cancellationToken);
        }

        return _inner.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options?.Tools is { Count: > 0 })
        {
            return _inner.GetStreamingResponseAsync(chatMessages, new ChatOptions(), cancellationToken);
        }

        return _inner.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => _inner.GetService(serviceType, serviceKey);
}
