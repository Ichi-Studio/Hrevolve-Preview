using Hrevolve.Agent.Configuration;
using Hrevolve.Agent.Models;
using Hrevolve.Agent.Models.Text2Sql;
using Hrevolve.Agent.Services;
using Hrevolve.Agent.Services.Metrics;
using Hrevolve.Agent.Services.Models;
using Hrevolve.Agent.Services.Routing;
using Hrevolve.Agent.Services.Text2Sql;
using Hrevolve.Agent.Tests.Fakes;
using Hrevolve.Shared.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hrevolve.Agent.Tests;

public sealed class HrAgentServiceRoutingTests
{
    [Fact]
    public async Task ChatAsync_Text2SqlRoute_ReturnsEnvelopeWithDiagnostics()
    {
        var employeeId = Guid.NewGuid();
        var currentUserAccessor = new CurrentUserAccessor
        {
            CurrentUser = new CurrentUser
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                TenantId = Guid.NewGuid(),
                Roles = ["user"],
                Permissions = ["hr:read"]
            }
        };

        var router = new FixedRouter(new RouteDecision(AgentRoute.Text2Sql, 0.9, "heuristic", "test"));

        var text2SqlService = new FixedText2SqlService(new QueryRequest
        {
            Operation = QueryOperation.Select,
            TargetEntity = "Employee",
            Limit = 10
        });

        var queryResult = QueryResult.Ok(
            data: [new Dictionary<string, object?> { ["Id"] = Guid.NewGuid(), ["Name"] = "张三" }],
            rowCount: 1,
            columns: [new ColumnInfo { Name = "Name", DisplayName = "姓名", DataType = "string", IsNullable = false }]
        );
        queryResult.ExecutionTimeMs = 12;
        queryResult.GeneratedSql = "SELECT 1";
        queryResult.Warnings = ["generated-sql-approx"];

        var queryExecutor = new FixedQueryExecutor(queryResult);

        var chatClientProvider = new FakeAgentChatClientProvider(new()
        {
            [ModelPurpose.Chat] = ("chat", new FakeChatClient((_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))))),
            [ModelPurpose.Text2Sql] = ("text2sql", new FakeChatClient((_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}"))))),
            [ModelPurpose.Router] = ("router", new FakeChatClient((_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"route\":\"text2sql\",\"confidence\":0.9}")))))
        });

        var toolProvider = new EmptyToolProvider();
        var metrics = new AgentMetrics();
        var options = Options.Create(new Text2SqlOptions { IncludeGeneratedSqlInResponse = true });

        var service = new HrAgentService(
            chatClientProvider,
            toolProvider,
            router,
            text2SqlService,
            queryExecutor,
            currentUserAccessor,
            options,
            metrics,
            NullLogger<HrAgentService>.Instance);

        var envelope = await service.ChatAsync(employeeId, "查询员工", CancellationToken.None);

        Assert.Equal("text2sql", envelope.Route);
        Assert.NotNull(envelope.Diagnostics);
        Assert.Equal(12, envelope.Diagnostics!.ExecutionMs);
        Assert.Equal("SELECT 1", envelope.Diagnostics.GeneratedSql);
        Assert.Contains("查询结果", envelope.Reply);
    }

    [Fact]
    public async Task ChatAsync_RequestCanceled_ReturnsCancelledEnvelope()
    {
        var employeeId = Guid.NewGuid();
        var currentUserAccessor = new CurrentUserAccessor
        {
            CurrentUser = new CurrentUser
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                TenantId = Guid.NewGuid(),
                Roles = ["user"],
                Permissions = ["hr:read"]
            }
        };

        var chatClientProvider = new FakeAgentChatClientProvider(new()
        {
            [ModelPurpose.Chat] = ("chat", new FakeChatClient((_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))))),
        });

        var service = new HrAgentService(
            chatClientProvider,
            new EmptyToolProvider(),
            new CancelingRouter(),
            new FixedText2SqlService(new QueryRequest { Operation = QueryOperation.Select, TargetEntity = "Employee", Limit = 1 }),
            new FixedQueryExecutor(QueryResult.Ok([], 0, [])),
            currentUserAccessor,
            Options.Create(new Text2SqlOptions()),
            new AgentMetrics(),
            NullLogger<HrAgentService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var envelope = await service.ChatAsync(employeeId, "hi", cts.Token);

        Assert.Equal("chat", envelope.Route);
        Assert.Equal("请求已取消。", envelope.Reply);
    }

    private sealed class EmptyToolProvider : IHrToolProvider
    {
        public IList<AITool> GetTools() => [];
    }

    private sealed class FixedRouter : IAgentSemanticRouter
    {
        private readonly RouteDecision _decision;
        public FixedRouter(RouteDecision decision) => _decision = decision;
        public Task<RouteDecision> RouteAsync(string message, IReadOnlyList<AgentChatMessage> recentHistory, CancellationToken cancellationToken = default)
            => Task.FromResult(_decision);
    }

    private sealed class CancelingRouter : IAgentSemanticRouter
    {
        public Task<RouteDecision> RouteAsync(string message, IReadOnlyList<AgentChatMessage> recentHistory, CancellationToken cancellationToken = default)
            => Task.FromCanceled<RouteDecision>(cancellationToken);
    }

    private sealed class FixedText2SqlService : IText2SqlService
    {
        private readonly QueryRequest _request;
        public FixedText2SqlService(QueryRequest request) => _request = request;

        public Task<Text2SqlResult> ConvertAsync(string naturalQuery, string? conversationContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Text2SqlResult.Ok(_request));

        public Task<bool> ValidateQueryIntentAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FixedQueryExecutor : IQueryExecutor
    {
        private readonly QueryResult _result;
        public FixedQueryExecutor(QueryResult result) => _result = result;

        public Task<QueryResult> ExecuteAsync(QueryRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
