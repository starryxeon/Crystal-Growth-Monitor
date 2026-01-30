using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using Crystal_Growth_Monitor.grpc;

namespace Crystal_Growth_Monitor;

public partial class App : Application
{
    public static FurnaceGrpcClient GrpcClient { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("Starting gRPC Client");
        GrpcClient = new FurnaceGrpcClient("http://192.168.168.96:5000");
        Console.WriteLine("gRPC client created");
        GrpcClient.Start();
        Console.WriteLine("gRPC client started");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += async (_, __) =>
            {
                await GrpcClient.StopAsync();
                await GrpcClient.DisposeAsync();
            };
        }

    }
}