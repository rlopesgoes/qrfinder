using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace Infrastructure.Telemetry;

public static class KafkaTraceContextPropagator
{
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";
    
    public static void InjectTraceContext(Headers headers)
    {
        var activity = Activity.Current;
        if (activity == null) return;
        
        var traceFlags = (int)(activity.ActivityTraceFlags & ActivityTraceFlags.Recorded);
        var traceParent = $"00-{activity.TraceId}-{activity.SpanId}-{traceFlags:x2}";
        headers.Add(TraceParentHeaderName, Encoding.UTF8.GetBytes(traceParent));
        
        if (!string.IsNullOrEmpty(activity.TraceStateString))
            headers.Add(TraceStateHeaderName, Encoding.UTF8.GetBytes(activity.TraceStateString));
        
        headers.Add("x-trace-id", Encoding.UTF8.GetBytes(activity.TraceId.ToString()));
        headers.Add("x-span-id", Encoding.UTF8.GetBytes(activity.SpanId.ToString()));
    }
    
    public static Activity? ExtractAndStartActivity(Headers headers, string operationName)
    {
        var activitySource = new ActivitySource("QrFinder.Kafka");
        
        var traceParentHeader = headers.FirstOrDefault(h => h.Key == TraceParentHeaderName);
        if (traceParentHeader?.GetValueBytes() == null)
            return activitySource.StartActivity(operationName);

        var traceParent = Encoding.UTF8.GetString(traceParentHeader.GetValueBytes());
        if (TryParseTraceParent(traceParent, out var traceId, out var parentSpanId, out var traceFlags))
        {
            var parentContext = new ActivityContext(traceId, parentSpanId, traceFlags);
            var traceStateHeader = headers.FirstOrDefault(h => h.Key == TraceStateHeaderName);
            var traceState = traceStateHeader?.GetValueBytes() != null 
                ? Encoding.UTF8.GetString(traceStateHeader.GetValueBytes()) 
                : null;

            if (!string.IsNullOrEmpty(traceState))
                parentContext = new ActivityContext(traceId, parentSpanId, traceFlags, traceState);

            return activitySource.StartActivity(operationName, ActivityKind.Consumer, parentContext);
        }

        return activitySource.StartActivity(operationName);
    }

    private static bool TryParseTraceParent(string traceParent, out ActivityTraceId traceId, out ActivitySpanId spanId, out ActivityTraceFlags traceFlags)
    {
        traceId = default;
        spanId = default;
        traceFlags = default;

        if (string.IsNullOrEmpty(traceParent) || traceParent.Length != 55)
            return false;

        var parts = traceParent.Split('-');
        if (parts.Length != 4 || parts[0] != "00")
            return false;

        try
        {
            traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
            spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
            traceFlags = (ActivityTraceFlags)Convert.ToByte(parts[3], 16);
            return true;
        }
        catch
        {
            return false;
        }
    }
}