using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OpenCvSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crystal_Growth_Monitor.rtsp;

public sealed class RtspCameraService : IDisposable
{
    private CancellationTokenSource? _cts;
    public event Action<WriteableBitmap>? FrameReady;

    public void Start(string rtspUrl)
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        Task.Run(() => CaptureLoopAsync(rtspUrl, _cts.Token));
    }

    private async Task CaptureLoopAsync(string rtspUrl, CancellationToken token)
    {
        string pipeline = $"rtspsrc location={rtspUrl} latency=0 ! rtph264depay ! h264parse ! avdec_h264 ! videoconvert ! appsink drop=true sync=false";

        bool warmedUp = false;

        while (!token.IsCancellationRequested)
        {
            if (!warmedUp)
            {
                try
                {
                    using var warmup = new VideoCapture(pipeline, VideoCaptureAPIs.GSTREAMER);
                    await Task.Delay(200, token);
                }
                catch { }

                warmedUp = true;
            }

            using var capture = new VideoCapture(pipeline, VideoCaptureAPIs.GSTREAMER);

            if (!capture.IsOpened())
            {
                await Task.Delay(300, token);
                continue;
            }

            using var frame = new Mat();
            using var bgra = new Mat();

            bool gotFrame = false;
            var start = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                if (!capture.Read(frame) || frame.Empty())
                {
                    if (!gotFrame && (DateTime.UtcNow - start).TotalMilliseconds > 800)
                        break;

                    await Task.Delay(10, token);
                    continue;
                }

                gotFrame = true;

                Cv2.CvtColor(frame, bgra, ColorConversionCodes.BGR2BGRA);
                var bitmap = CreateBitmapFromMat(bgra);

                Dispatcher.UIThread.Post(() => FrameReady?.Invoke(bitmap), DispatcherPriority.MaxValue);
            }

            await Task.Delay(300, token);
        }
    }


    private unsafe WriteableBitmap CreateBitmapFromMat(Mat frame)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            long size = (long)frame.Step() * frame.Height;
            Buffer.MemoryCopy((void*)frame.Data, (void*)fb.Address, size, size);
        }
        return bitmap;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    public void Dispose() => Stop();
}
