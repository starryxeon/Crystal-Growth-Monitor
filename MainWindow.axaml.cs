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
}
