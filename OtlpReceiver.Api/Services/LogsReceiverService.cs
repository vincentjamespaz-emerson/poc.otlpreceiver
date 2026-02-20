using Grpc.Core;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace OtlpReceiver.Api.Services;

public class LogsReceiverService : LogsService.LogsServiceBase
{
    private readonly ILogger<LogsReceiverService> _logger;

    public LogsReceiverService(ILogger<LogsReceiverService> logger)
    {
        _logger = logger;
    }

    public override Task<ExportLogsServiceResponse> Export(
        ExportLogsServiceRequest request,
        ServerCallContext context)
    {
        int logRecordCount = 0;
        foreach (var resourceLogs in request.ResourceLogs)
        {
            var resource = resourceLogs.Resource;
            var serviceName = resource?.Attributes
                .FirstOrDefault(a => a.Key == "service.name")?.Value?.StringValue ?? "unknown";

            foreach (var scopeLogs in resourceLogs.ScopeLogs)
            {
                logRecordCount += scopeLogs.LogRecords.Count;
                foreach (var logRecord in scopeLogs.LogRecords)
                {
                    var severity = logRecord.SeverityText;
                    var body = logRecord.Body?.StringValue ?? string.Empty;
                    _logger.LogInformation(
                        "Received log record: Service={ServiceName}, Severity={Severity}, Body={Body}",
                        serviceName,
                        severity,
                        body);
                }
            }
        }

        _logger.LogInformation("Exported {LogRecordCount} log records from {ResourceCount} resources (gRPC)",
            logRecordCount, request.ResourceLogs.Count);

        return Task.FromResult(new ExportLogsServiceResponse());
    }
}
