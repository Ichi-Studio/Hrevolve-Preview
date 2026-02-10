namespace Hrevolve.Agent.Services.Models;

public sealed class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("MockChatClient", new Uri("http://localhost"), "mock-model");

    public void Dispose() { }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(250, cancellationToken);

        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";

        var response = lastUserMessage.ToLowerInvariant() switch
        {
            var s when s.Contains("假期") || s.Contains("年假") =>
                "您好！我来帮您查询假期余额。根据系统记录，您当前的年假余额为5天，病假余额为10天。如需请假，请告诉我具体的日期和原因。",
            var s when s.Contains("薪资") || s.Contains("工资") =>
                "您好！本月薪资已于15日发放，实发金额为15,300元。如需查看详细明细，请告诉我。",
            var s when s.Contains("考勤") || s.Contains("打卡") =>
                "您好！今日您已于09:02完成签到。如需查看历史考勤记录，请告诉我查询的时间范围。",
            var s when s.Contains("请假") =>
                "好的，我来帮您提交请假申请。请告诉我：1. 请假类型（年假/病假/事假）2. 开始日期 3. 结束日期 4. 请假原因",
            var s when s.Contains("组织") || s.Contains("部门") =>
                "我来为您查询组织架构信息。请问您想了解哪个部门的信息？",
            _ => "您好！我是Hrevolve HR助手，可以帮您查询假期余额、薪资信息、考勤记录，也可以协助您提交请假申请。请问有什么可以帮您的？"
        };

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Mock client does not support streaming");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
