using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace OtlpReceiver.Tests;

public class OtlpHttpEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string ProtobufContentType = "application/x-protobuf";

    public OtlpHttpEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Root_ReturnsHealthMessage()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("OTLP Receiver is running", content);
    }

    [Fact]
    public async Task Post_Traces_ReturnsOk()
    {
        var request = new ExportTraceServiceRequest();
        request.ResourceSpans.Add(new ResourceSpans
        {
            Resource = new Resource
            {
                Attributes = { new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "test-service" } } }
            },
            ScopeSpans =
            {
                new ScopeSpans
                {
                    Spans =
                    {
                        new Span
                        {
                            TraceId = Google.Protobuf.ByteString.CopyFrom(new byte[16]),
                            SpanId = Google.Protobuf.ByteString.CopyFrom(new byte[8]),
                            Name = "test-span",
                            Kind = Span.Types.SpanKind.Internal
                        }
                    }
                }
            }
        });

        var response = await PostProtobuf("/v1/traces", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var responseProto = ExportTraceServiceResponse.Parser.ParseFrom(responseBytes);
        Assert.NotNull(responseProto);
    }

    [Fact]
    public async Task Post_Metrics_ReturnsOk()
    {
        var request = new ExportMetricsServiceRequest();
        request.ResourceMetrics.Add(new ResourceMetrics
        {
            Resource = new Resource
            {
                Attributes = { new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "test-service" } } }
            },
            ScopeMetrics =
            {
                new ScopeMetrics
                {
                    Metrics =
                    {
                        new OpenTelemetry.Proto.Metrics.V1.Metric
                        {
                            Name = "test.counter",
                            Description = "A test counter"
                        }
                    }
                }
            }
        });

        var response = await PostProtobuf("/v1/metrics", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var responseProto = ExportMetricsServiceResponse.Parser.ParseFrom(responseBytes);
        Assert.NotNull(responseProto);
    }

    [Fact]
    public async Task Post_Logs_ReturnsOk()
    {
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(new ResourceLogs
        {
            Resource = new Resource
            {
                Attributes = { new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "test-service" } } }
            },
            ScopeLogs =
            {
                new ScopeLogs
                {
                    LogRecords =
                    {
                        new LogRecord
                        {
                            SeverityText = "INFO",
                            Body = new AnyValue { StringValue = "Test log message" }
                        }
                    }
                }
            }
        });

        var response = await PostProtobuf("/v1/logs", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        var responseProto = ExportLogsServiceResponse.Parser.ParseFrom(responseBytes);
        Assert.NotNull(responseProto);
    }

    private async Task<HttpResponseMessage> PostProtobuf<T>(string path, T message) where T : IMessage<T>
    {
        var bytes = message.ToByteArray();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(ProtobufContentType);
        return await _client.PostAsync(path, content);
    }
}
