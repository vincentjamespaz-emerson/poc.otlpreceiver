using Grpc.Core;
using OpenTelemetry.Proto.Collector.Metrics.V1;

namespace OtlpReceiver.Api.Services;

public class MetricsReceiverService : MetricsService.MetricsServiceBase
{
    private readonly ILogger<MetricsReceiverService> _logger;

    public MetricsReceiverService(ILogger<MetricsReceiverService> logger)
    {
        _logger = logger;
    }

    public override Task<ExportMetricsServiceResponse> Export(
        ExportMetricsServiceRequest request,
        ServerCallContext context)
    {
        int metricCount = 0;
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var resource = resourceMetrics.Resource;
            var serviceName = resource?.Attributes
                .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

            foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
            {
                metricCount += scopeMetrics.Metrics.Count;
                foreach (var metric in scopeMetrics.Metrics)
                {
                    _logger.LogInformation(
                        "Received metric: Service={ServiceName}, Name={MetricName}, Description={Description}",
                        serviceName,
                        metric.Name,
                        metric.Description);
                }
            }
        }

        _logger.LogInformation("Exported {MetricCount} metrics from {ResourceCount} resources (gRPC)",
            metricCount, request.ResourceMetrics.Count);

        return Task.FromResult(new ExportMetricsServiceResponse());
    }
}
