using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Crystal_Growth_Monitor.grpc;

namespace Crystal_Growth_Monitor;

public partial class App : Application
{
    //public static FurnaceGrpcClient GrpcClient { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        //GrpcClient = new FurnaceGrpcClient("http://localhost:5000");
        //GrpcClient.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += async (_, __) =>
            {
                //await GrpcClient.StopAsync();
                //await GrpcClient.DisposeAsync();
            };
        }

    }
}