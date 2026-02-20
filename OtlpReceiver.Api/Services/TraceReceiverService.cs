using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace OtlpReceiver.Api.Services;

public class TraceReceiverService : TraceService.TraceServiceBase
{
    private readonly ILogger<TraceReceiverService> _logger;

    public TraceReceiverService(ILogger<TraceReceiverService> logger)
    {
        _logger = logger;
    }

    public override Task<ExportTraceServiceResponse> Export(
        ExportTraceServiceRequest request,
        ServerCallContext context)
    {
        int spanCount = 0;
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var resource = resourceSpans.Resource;
            var serviceName = resource?.Attributes
                .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

            foreach (var scopeSpans in resourceSpans.ScopeSpans)
            {
                spanCount += scopeSpans.Spans.Count;
                foreach (var span in scopeSpans.Spans)
                {
                    _logger.LogInformation(
                        "Received trace span: Service={ServiceName}, TraceId={TraceId}, SpanId={SpanId}, Name={SpanName}, Kind={Kind}",
                        serviceName,
                        Convert.ToHexString(span.TraceId.ToByteArray()),
                        Convert.ToHexString(span.SpanId.ToByteArray()),
                        span.Name,
                        span.Kind);
                }
            }
        }

        _logger.LogInformation("Exported {SpanCount} spans from {ResourceCount} resources (gRPC)",
            spanCount, request.ResourceSpans.Count);

        return Task.FromResult(new ExportTraceServiceResponse());
    }
}
