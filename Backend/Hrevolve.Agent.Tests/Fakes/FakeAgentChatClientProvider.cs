using Hrevolve.Agent.Services.Models;
using Microsoft.Extensions.AI;

namespace Hrevolve.Agent.Tests.Fakes;

public sealed class FakeAgentChatClientProvider : IAgentChatClientProvider
{
    private readonly Dictionary<ModelPurpose, (string ModelName, IChatClient Client)> _map;

    public FakeAgentChatClientProvider(Dictionary<ModelPurpose, (string ModelName, IChatClient Client)> map)
    {
        _map = map;
    }

    public IChatClient GetClient(ModelPurpose purpose)
    {
        return _map[purpose].Client;
    }

    public string GetModelName(ModelPurpose purpose)
    {
        return _map[purpose].ModelName;
    }
}
