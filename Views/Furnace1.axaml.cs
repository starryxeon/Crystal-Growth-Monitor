using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Crystal_Growth_Monitor.grpc;
using Crystal_Growth_Monitor.rtsp;

namespace Crystal_Growth_Monitor.Views;

public partial class Furnace1 : UserControl 
{
    public bool disabled = true;
    public string? copiedText;
    FurnaceGrpcClient client;

    private readonly RtspCameraService _camera = new();

    public Furnace1()
    {
        InitializeComponent();
        client = App.GrpcClient;

        _camera.FrameReady += bitmap =>
        {
            CameraImage.Source = bitmap;
        };
    
        _camera.Start("rtsp://192.168.168.202:8554/cam");
    }

    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _camera.Dispose();
        base.OnDetachedFromVisualTree(e);
    }

    private void CopyText_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OutputText.Text = InputBox.Text;
        copiedText = OutputText.Text;
    }
    
    private async void ChangeColor_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        btn.IsEnabled = false;
        try
        {
            disabled = !disabled;
            ColorCircle.Fill = disabled ? Brushes.Red : Brushes.Green;
            Console.WriteLine($"Value of 'disabled' variable: {disabled}");
            var resp = await client.SendEventAsync(new Event
            {
                Type = 9,
                Index = 0,
                Payload = ""
            });
            Console.WriteLine(resp);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }

}