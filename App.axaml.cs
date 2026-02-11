using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Crystal_Growth_Monitor.grpc;
using Crystal_Growth_Monitor.Views;
using Crystal_Growth_Monitor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using System.Threading.Tasks;


namespace Crystal_Growth_Monitor;

public partial class App : Application
{
    public static FurnaceGrpcClient GrpcClient { get; private set; } = null!;
    public FactoryContainer container;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        GrpcClient = new FurnaceGrpcClient("http://192.168.168.96:5000", ProcessFrame);
        GrpcClient.Start();
        

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

    /// <summary>
    /// Provided as callback to gRPC client to process an incoming frame. Should update all tabs and windows with new information.
    /// </summary>
    public ValueTask ProcessFrame(Frame frame)
    {   
        container.Update(frame);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (IAsyncUpdatable w in desktop.Windows)
                Dispatcher.UIThread.Post(async () =>
                {
                    try { w.UpdateAsync(container); }
                    catch (Exception ex) {Console.WriteLine(ex);}
                });
            return ValueTask.CompletedTask;
        }
        return ValueTask.CompletedTask;
    }
}