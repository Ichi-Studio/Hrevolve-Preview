using System.Diagnostics.Metrics;
using Hrevolve.Agent.Services.Models;
using Hrevolve.Agent.Services.Routing;

namespace Hrevolve.Agent.Services.Metrics;

public sealed class AgentMetrics
{
    private readonly Meter _meter = new("Hrevolve.Agent", "1.0.0");

    private readonly Counter<long> _routeTotal;
    private readonly Histogram<double> _modelLatencyMs;
    private readonly Counter<long> _modelErrorsTotal;
    private readonly Counter<long> _fallbackTotal;

    public AgentMetrics()
    {
        _routeTotal = _meter.CreateCounter<long>("agent_route_total");
        _modelLatencyMs = _meter.CreateHistogram<double>("model_latency_ms");
        _modelErrorsTotal = _meter.CreateCounter<long>("model_errors_total");
        _fallbackTotal = _meter.CreateCounter<long>("fallback_total");
    }

    public void RecordRoute(AgentRoute route, string strategy)
    {
        _routeTotal.Add(1, new[]
        {
            new KeyValuePair<string, object?>("route", route.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("strategy", strategy)
        });
    }

    public void RecordModelLatency(ModelPurpose purpose, double milliseconds, string? provider = null, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            _modelLatencyMs.Record(milliseconds, new[]
            {
                new KeyValuePair<string, object?>("purpose", purpose.ToString().ToLowerInvariant())
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            _modelLatencyMs.Record(milliseconds, new[]
            {
                new KeyValuePair<string, object?>("purpose", purpose.ToString().ToLowerInvariant()),
                new KeyValuePair<string, object?>("provider", provider)
            });
            return;
        }

        _modelLatencyMs.Record(milliseconds, new[]
        {
            new KeyValuePair<string, object?>("purpose", purpose.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("model", model)
        });
    }

    public void RecordModelError(ModelPurpose purpose, string? provider = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            _modelErrorsTotal.Add(1, new[]
            {
                new KeyValuePair<string, object?>("purpose", purpose.ToString().ToLowerInvariant())
            });
            return;
        }

        _modelErrorsTotal.Add(1, new[]
        {
            new KeyValuePair<string, object?>("purpose", purpose.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("provider", provider)
        });
    }

    public void RecordFallback(string fromProvider, string toProvider, string reason)
    {
        _fallbackTotal.Add(1, new[]
        {
            new KeyValuePair<string, object?>("from", fromProvider),
            new KeyValuePair<string, object?>("to", toProvider),
            new KeyValuePair<string, object?>("reason", reason)
        });
    }
}
