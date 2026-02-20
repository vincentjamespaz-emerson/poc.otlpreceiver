using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace OtlpReceiver.Api.Endpoints;

public static class OtlpHttpEndpoints
{
    private const string ProtobufContentType = "application/x-protobuf";

    public static void MapOtlpHttpEndpoints(this WebApplication app)
    {
        // OTLP HTTP/Protobuf trace endpoint
        app.MapPost("/v1/traces", async (HttpContext context, ILogger<Program> logger) =>
        {
            var request = await ParseRequest<ExportTraceServiceRequest>(context);
            if (request is null)
                return Results.BadRequest("Invalid protobuf payload");

            int spanCount = 0;
            foreach (var resourceSpans in request.ResourceSpans)
            {
                var serviceName = resourceSpans.Resource?.Attributes
                    .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

                foreach (var scopeSpans in resourceSpans.ScopeSpans)
                {
                    spanCount += scopeSpans.Spans.Count;
                    foreach (var span in scopeSpans.Spans)
                    {
                        logger.LogInformation(
                            "Received trace span: Service={ServiceName}, TraceId={TraceId}, SpanId={SpanId}, Name={SpanName}, Kind={Kind}",
                            serviceName,
                            Convert.ToHexString(span.TraceId.ToByteArray()),
                            Convert.ToHexString(span.SpanId.ToByteArray()),
                            span.Name,
                            span.Kind);
                    }
                }
            }

            logger.LogInformation("Exported {SpanCount} spans from {ResourceCount} resources (HTTP)",
                spanCount, request.ResourceSpans.Count);

            return WriteProtobufResponse(context, new ExportTraceServiceResponse());
        });

        // OTLP HTTP/Protobuf metrics endpoint
        app.MapPost("/v1/metrics", async (HttpContext context, ILogger<Program> logger) =>
        {
            var request = await ParseRequest<ExportMetricsServiceRequest>(context);
            if (request is null)
                return Results.BadRequest("Invalid protobuf payload");

            int metricCount = 0;
            foreach (var resourceMetrics in request.ResourceMetrics)
            {
                var serviceName = resourceMetrics.Resource?.Attributes
                    .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

                foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
                {
                    metricCount += scopeMetrics.Metrics.Count;
                    foreach (var metric in scopeMetrics.Metrics)
                    {
                        logger.LogInformation(
                            "Received metric: Service={ServiceName}, Name={MetricName}, Description={Description}",
                            serviceName,
                            metric.Name,
                            metric.Description);
                    }
                }
            }

            logger.LogInformation("Exported {MetricCount} metrics from {ResourceCount} resources (HTTP)",
                metricCount, request.ResourceMetrics.Count);

            return WriteProtobufResponse(context, new ExportMetricsServiceResponse());
        });

        // OTLP HTTP/Protobuf logs endpoint
        app.MapPost("/v1/logs", async (HttpContext context, ILogger<Program> logger) =>
        {
            var request = await ParseRequest<ExportLogsServiceRequest>(context);
            if (request is null)
                return Results.BadRequest("Invalid protobuf payload");

            int logRecordCount = 0;
            foreach (var resourceLogs in request.ResourceLogs)
            {
                var serviceName = resourceLogs.Resource?.Attributes
                    .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

                foreach (var scopeLogs in resourceLogs.ScopeLogs)
                {
                    logRecordCount += scopeLogs.LogRecords.Count;
                    foreach (var logRecord in scopeLogs.LogRecords)
                    {
                        logger.LogInformation(
                            "Received log record: Service={ServiceName}, Severity={Severity}, Body={Body}",
                            serviceName,
                            logRecord.SeverityText,
                            logRecord.Body?.StringValue ?? string.Empty);
                    }
                }
            }

            logger.LogInformation("Exported {LogRecordCount} log records from {ResourceCount} resources (HTTP)",
                logRecordCount, request.ResourceLogs.Count);

            return WriteProtobufResponse(context, new ExportLogsServiceResponse());
        });
    }

    private static async Task<T?> ParseRequest<T>(HttpContext context) where T : IMessage<T>, new()
    {
        try
        {
            using var stream = context.Request.Body;
            var bytes = await ReadAllBytesAsync(stream);
            var parser = new MessageParser<T>(() => new T());
            return parser.ParseFrom(bytes);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetService<ILogger<Program>>();
            logger?.LogWarning(ex, "Failed to parse OTLP protobuf request for {Path}", context.Request.Path);
            return default;
        }
    }

    private static IResult WriteProtobufResponse<T>(HttpContext context, T message) where T : IMessage<T>
    {
        var bytes = message.ToByteArray();
        return Results.Bytes(bytes, ProtobufContentType);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
