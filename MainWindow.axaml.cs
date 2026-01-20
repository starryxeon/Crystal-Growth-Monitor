using Avalonia.Controls;
using Avalonia.Media;
using Crystal_Growth_Monitor.grpc;
using Grpc.Net.ClientFactory;
using System;
using System.Threading.Tasks;

namespace Crystal_Growth_Monitor
{
    public partial class MainWindow : Window
    {
        public bool disabled = true;
        public string? copiedText;
        FurnaceGrpcClient client;

        public MainWindow()
        {
            InitializeComponent();
            client = App.GrpcClient;
        }

        private void CopyText_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OutputText.Text = InputBox.Text;
            copiedText = OutputText.Text;
        }

        private void ChangeColor_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            disabled = !disabled;
            ColorCircle.Fill = disabled ? Brushes.Red : Brushes.Green;
            Console.WriteLine($"Value of 'disabled' variable: {disabled}");
            client.eventIn.WriteAsync(new Event
            {
                Type = 6,
                Index = 0,
                Payload = "client says hello"
            });
        }
    }
}
