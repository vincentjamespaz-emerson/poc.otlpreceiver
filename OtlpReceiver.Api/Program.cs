using OtlpReceiver.Api.Endpoints;
using OtlpReceiver.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

var app = builder.Build();

// Health check endpoint
app.MapGet("/", () => "OTLP Receiver is running. Use /v1/traces, /v1/metrics, or /v1/logs for HTTP, or gRPC on the same port.");

// Map gRPC services (OTLP gRPC endpoints)
app.MapGrpcService<TraceReceiverService>();
app.MapGrpcService<MetricsReceiverService>();
app.MapGrpcService<LogsReceiverService>();

// Map HTTP/Protobuf OTLP endpoints
app.MapOtlpHttpEndpoints();

app.Run();

// Make the implicit Program class accessible to test projects
public partial class Program { }
