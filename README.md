# poc.otlpreceiver

An External OTLP (OpenTelemetry Protocol) Receiver built with ASP.NET Core.

## Overview

This project implements an OpenTelemetry Protocol (OTLP) receiver that accepts telemetry data (traces, metrics, and logs) from any OpenTelemetry-instrumented application. It supports both:

- **gRPC** transport (HTTP/2) — standard OTLP gRPC services
- **HTTP/Protobuf** transport — standard OTLP HTTP endpoints

All received telemetry is logged using the ASP.NET Core logging infrastructure. The architecture is easily extensible to forward data to backends such as Jaeger, Prometheus, Loki, or any other observability platform.

## Project Structure

```
OtlpReceiver.Api/          # ASP.NET Core Web API
├── Endpoints/
│   └── OtlpHttpEndpoints.cs   # OTLP HTTP/Protobuf endpoints
├── Services/
│   ├── TraceReceiverService.cs    # gRPC TraceService implementation
│   ├── MetricsReceiverService.cs  # gRPC MetricsService implementation
│   └── LogsReceiverService.cs     # gRPC LogsService implementation
├── Protos/                        # OpenTelemetry proto definitions
└── Program.cs                     # Application entry point

OtlpReceiver.Tests/        # xUnit integration tests
```

## Endpoints

### HTTP/Protobuf (OTLP HTTP)

| Method | Path         | Description                    |
|--------|--------------|--------------------------------|
| POST   | `/v1/traces` | Receive trace spans            |
| POST   | `/v1/metrics`| Receive metrics                |
| POST   | `/v1/logs`   | Receive log records            |

Content-Type: `application/x-protobuf`

### gRPC (OTLP gRPC)

| Service                                          | Method   | Description         |
|--------------------------------------------------|----------|---------------------|
| `opentelemetry.proto.collector.trace.v1.TraceService`   | `Export` | Receive trace spans |
| `opentelemetry.proto.collector.metrics.v1.MetricsService` | `Export` | Receive metrics  |
| `opentelemetry.proto.collector.logs.v1.LogsService`     | `Export` | Receive log records |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project OtlpReceiver.Api
```

The receiver will start on `http://localhost:5150` (HTTP) and `https://localhost:7171` (HTTPS) by default.

### Test

```bash
dotnet test
```

## Connecting an OpenTelemetry Exporter

Configure your application's OTLP exporter to send data to this receiver.

### HTTP example (OpenTelemetry .NET SDK)

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:5150/v1/traces");
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
    })
    .Build();
```

### gRPC example (OpenTelemetry .NET SDK)

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:5150");
        options.Protocol = OtlpExportProtocol.Grpc;
    })
    .Build();
```

### Environment variables

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5150
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf   # or grpc
```
