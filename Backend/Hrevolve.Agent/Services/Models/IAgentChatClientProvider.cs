namespace Hrevolve.Agent.Services.Models;

public interface IAgentChatClientProvider
{
    IChatClient GetClient(ModelPurpose purpose);

    string GetModelName(ModelPurpose purpose);
}
