using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Crystal_Growth_Monitor.grpc;
using Crystal_Growth_Monitor.Views;
using Crystal_Growth_Monitor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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

        var services = new ServiceCollection();
        services.AddSingleton<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        var vm = Ioc.Default.GetRequiredService<MainWindowViewModel>();


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(vm);
            desktop.Exit += async (_, __) =>
            {
                await GrpcClient.StopAsync();
                await GrpcClient.DisposeAsync();
            };
        }

    }
}