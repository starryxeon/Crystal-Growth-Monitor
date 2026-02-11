using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;

namespace Crystal_Growth_Monitor.grpc;
using Crystal_Growth_Monitor;

public sealed class FurnaceGrpcClient : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly StreamService.StreamServiceClient _streamClient;
    private readonly Events.EventsClient _eventsClient;

    private readonly CancellationTokenSource _cts = new();

    private AsyncDuplexStreamingCall<Frame, Frame>? _frameCall;
    private Task? _frameReceiveTask;
    private readonly SemaphoreSlim _frameWriteLock = new(1, 1);

    private readonly Func<Frame, ValueTask> _onFrameReceived;

    public FurnaceGrpcClient(string address, Func<Frame, ValueTask> onFrameReceived, GrpcChannelOptions? options = null)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _channel = GrpcChannel.ForAddress(address, options ?? new GrpcChannelOptions());
        _streamClient = new StreamService.StreamServiceClient(_channel);
        _eventsClient = new Events.EventsClient(_channel);

        _onFrameReceived = onFrameReceived;
    }

    /// <summary>
    /// Open the bidirectional stream and start receiving frames.
    /// </summary>
    public void Start()
    {
        if (_frameCall != null) throw new InvalidOperationException("Client already started.");

        _frameCall = _streamClient.Stream(cancellationToken: _cts.Token);
        _frameReceiveTask = ReceiveFramesLoopAsync(_frameCall.ResponseStream, _cts.Token);
    }

    /// <summary>
    /// Send one frame on the stream.
    /// </summary>
    public async Task SendFrameAsync(Frame frame, CancellationToken ct = default)
    {
        var call = _frameCall ?? throw new InvalidOperationException("Call Start() first.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        // lock to avoid concurrent writes
        await _frameWriteLock.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            await call.RequestStream.WriteAsync(frame).ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
        finally
        {
            _frameWriteLock.Release();
        }
    }

    /// <summary>
    /// Send one event and return the response.
    /// </summary>
    public async Task<EventResponse> SendEventAsync(Event ev, CancellationToken ct = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);

        try
        {
            return await _eventsClient.SendEventAsync(ev, cancellationToken: linked.Token)
                                      .ResponseAsync
                                      .ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            throw;
        }
    }

    private async Task ReceiveFramesLoopAsync(IAsyncStreamReader<Frame> responseStream, CancellationToken ct)
    {
        try
        {
            while (await responseStream.MoveNext(ct).ConfigureAwait(false))
            {
                await _onFrameReceived(responseStream.Current).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_frameCall != null)
        {
            try { await _frameCall.RequestStream.CompleteAsync().ConfigureAwait(false); }
            catch {}
        }

        if (_frameReceiveTask != null)
        {
            try { await _frameReceiveTask.ConfigureAwait(false); }
            catch {}
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _frameWriteLock.Dispose();
        _cts.Dispose();
        _channel.Dispose();
    }
}
