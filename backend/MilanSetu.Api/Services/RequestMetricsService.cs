using System.Collections.Concurrent;
using System.Diagnostics;

namespace MilanSetu.Api.Services;

public sealed class RequestMetricsService
{
    private readonly ConcurrentDictionary<string, Metric> _metrics = new(StringComparer.OrdinalIgnoreCase);
    private long _totalRequests;
    private long _failedRequests;

    public void Record(string method, string path, int statusCode, long elapsedMs)
    {
        Interlocked.Increment(ref _totalRequests);
        if (statusCode >= 500) Interlocked.Increment(ref _failedRequests);
        var key = $"{method} {path}";
        var metric = _metrics.GetOrAdd(key, _ => new Metric());
        Interlocked.Increment(ref metric.Count);
        if (statusCode >= 500) Interlocked.Increment(ref metric.Errors);
        Interlocked.Add(ref metric.ElapsedMs, elapsedMs);
        if (elapsedMs > Volatile.Read(ref metric.MaxMs))
            Interlocked.Exchange(ref metric.MaxMs, elapsedMs);
    }

    public object Snapshot()
    {
        var total = Volatile.Read(ref _totalRequests);
        var failed = Volatile.Read(ref _failedRequests);
        return new
        {
            capturedAt = DateTimeOffset.UtcNow,
            totalRequests = total,
            failedRequests = failed,
            failureRatePercent = total == 0 ? 0 : Math.Round(failed * 100d / total, 2),
            endpoints = _metrics.OrderByDescending(x => x.Value.Count).Take(50).Select(x => new
            {
                endpoint = x.Key, requests = Volatile.Read(ref x.Value.Count), errors = Volatile.Read(ref x.Value.Errors),
                averageMs = x.Value.Count == 0 ? 0 : Math.Round(x.Value.ElapsedMs / (double)x.Value.Count, 1),
                maxMs = Volatile.Read(ref x.Value.MaxMs)
            })
        };
    }

    private sealed class Metric
    {
        public long Count;
        public long Errors;
        public long ElapsedMs;
        public long MaxMs;
    }
}

public sealed class RequestMetricsMiddleware(RequestDelegate next, RequestMetricsService metrics, ILogger<RequestMetricsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            metrics.Record(context.Request.Method, context.Request.Path.Value ?? "/", context.Response.StatusCode, elapsedMs);
            if (context.Response.StatusCode >= 500)
                logger.LogWarning("MilanSetu request failed. CorrelationId={CorrelationId} Method={Method} Path={Path} Status={StatusCode} ElapsedMs={ElapsedMs}", correlationId, context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);
        }
    }
}