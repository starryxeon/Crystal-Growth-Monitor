using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;

namespace Crystal_Growth_Monitor.grpc;

public sealed class FurnaceGrpcClient : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly StreamService.StreamServiceClient _streamClient;
    private readonly Events.EventsClient _eventsClient;

    private readonly CancellationTokenSource _cts = new();
    private Task? _framesTask;
    private Task? _eventsTask;
    private readonly Channel<Event> _eventChannel;
    private readonly Channel<EventResponse> _eventResponseChannel;
    private readonly Channel<Frame> _frameChannel;
    private readonly Channel<Frame> _frameResponseChannel;

    public ChannelWriter<Event> eventIn => _eventChannel.Writer;
    public ChannelReader<EventResponse> eventOut => _eventResponseChannel.Reader;

    public ChannelWriter<Frame> frameIn => _frameChannel.Writer;
    public ChannelReader<Frame> frameOut => _frameResponseChannel.Reader;




    public FurnaceGrpcClient(string address, GrpcChannelOptions? options = null)
    {
        // If your server is plaintext h2c (http://), you must enable HTTP/2 without TLS:
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        //
        // Do that once at process start (recommended) or before creating the channel.
        _channel = GrpcChannel.ForAddress(address, options ?? new GrpcChannelOptions());

        _streamClient = new StreamService.StreamServiceClient(_channel);
        _eventsClient = new Events.EventsClient(_channel);
        
        _eventChannel = Channel.CreateBounded<Event>(new BoundedChannelOptions(capacity: 10_000)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _eventResponseChannel = Channel.CreateBounded<EventResponse>(new BoundedChannelOptions(capacity: 10_000)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        _frameChannel = Channel.CreateBounded<Frame>(new BoundedChannelOptions(capacity: 1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _frameResponseChannel = Channel.CreateBounded<Frame>(new BoundedChannelOptions(capacity: 1)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <summary>
    /// Starts background loops to publish frames and events from the client's channel children.
    /// </summary>
    public void Start()
    {
        if (_framesTask != null || _eventsTask != null)
            throw new InvalidOperationException("Client already started.");
            
        _framesTask = RunFramesAsync(_frameChannel.Reader, _frameResponseChannel.Writer, _cts.Token);
        //_eventsTask = RunEventsAsync(_eventChannel.Reader, _eventResponseChannel.Writer, _cts.Token);

    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _cts.Cancel();
        if (_framesTask != null) await SwallowStopAsync(_framesTask).ConfigureAwait(false);
        if (_eventsTask != null) await SwallowStopAsync(_eventsTask).ConfigureAwait(false);

        static async Task SwallowStopAsync(Task t)
        {
            try { await t.ConfigureAwait(false); }
            catch { /* log if desired */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
        _channel.Dispose();
    }

    private async Task RunFramesAsync(
        ChannelReader<Frame> input,
        ChannelWriter<Frame> output,
        CancellationToken ct)
    {
        // Establish bidi stream call
        using var call = _streamClient.Stream(cancellationToken: ct);

        // Two concurrent loops: one sending frames, one reading responses.
        var sendTask = SendNewestFramesAsync(input, call.RequestStream, ct);
        var recvTask = ReadFramesAsync(call.ResponseStream, output, ct);

        await Task.WhenAll(sendTask, recvTask).ConfigureAwait(false);
    }

    private static async Task SendNewestFramesAsync(
        ChannelReader<Frame> input,
        IClientStreamWriter<Frame> requestStream,
        CancellationToken ct)
    {
        try
        {
            while (await input.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                Frame newest = await input.ReadAsync();
                await requestStream.WriteAsync(newest).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
        finally
        {
            // Signal completion of client->server stream
            try { await requestStream.CompleteAsync().ConfigureAwait(false); }
            catch { /* ignore during shutdown */ }
        }
    }

    private static async Task ReadFramesAsync(
        IAsyncStreamReader<Frame> responseStream,
        ChannelWriter<Frame> output,
        CancellationToken ct)
    {
        try
        {
            while (await responseStream.MoveNext(ct).ConfigureAwait(false))
            {
                var msg = responseStream.Current;
                await output.WriteAsync(msg, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
        finally
        {
            output.TryComplete();
        }
    }

    private async Task RunEventsAsync(
        ChannelReader<Event> input,
        ChannelWriter<EventResponse> output,
        CancellationToken ct)
    {
        try
        {
            await foreach (var ev in input.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var resp = await _eventsClient.SendEventAsync(ev, cancellationToken: ct)
                                              .ResponseAsync
                                              .ConfigureAwait(false);

                await output.WriteAsync(resp, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC error: {ex.StatusCode} - {ex.Status.Detail}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Event loop crashed: {ex}");
            throw;
        }

        finally
        {
            output.TryComplete();
        }
    }

    public async Task<EventResponse> SendEventAsync(Event ev, CancellationToken ct = default)
    {
        try
        {
            return await _eventsClient.SendEventAsync(ev, cancellationToken: ct)
                                      .ResponseAsync
                                      .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            throw;
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"gRPC error: {ex.StatusCode} - {ex.Status.Detail}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendEvent crashed: {ex}");
            throw;
        }
    }
}
