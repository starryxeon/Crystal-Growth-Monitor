using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using Grpc.Core;
using Grpc.AspNetCore;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Crystal_Growth_Monitor.grpc;

public class StreamServiceImpl : StreamService.StreamServiceBase
{
    public override async Task Stream(
        IAsyncStreamReader<Frame> requestStream,
        IServerStreamWriter<Frame> responseStream,
        ServerCallContext context)
    {
        await foreach (var frame in requestStream.ReadAllAsync())
        {
            var response = new Frame
            {
                Seq = frame.Seq,
                Payload = $"Server received: {frame.Payload}"
            };

            await responseStream.WriteAsync(response);
        }
    }
}

public sealed class EventsSender : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly Events.EventsClient _client;

    public EventsSender(string serverAddress)
    {
        // serverAddress example:
        //   https://localhost:5001   (typical ASP.NET Core gRPC w/ TLS dev cert)
        //   http://localhost:5000    (plaintext, if configured)
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new Events.EventsClient(_channel);
    }

    /// <summary>
    /// Sends one event and returns the server's response.
    /// </summary>
    public async Task<EventResponse> SendAsync(
        EventType type,
        uint index,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var request = new Event
        {
            Type = type,
            Index = index,
            Payload = payload ?? ""
        };

        try
        {
            // You can set deadlines if you want:
            // var deadline = DateTime.UtcNow.AddSeconds(5);
            // return await _client.SendEventAsync(request, deadline: deadline, cancellationToken: cancellationToken);

            return await _client.SendEventAsync(request, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            // Typical transient errors: server down, network blip, timeout
            throw new InvalidOperationException(
                $"Failed to send event (Type={type}, Index={index}): {ex.Status.Detail}", ex);
        }
    }

    public void Dispose() => _channel.Dispose();
    
}
