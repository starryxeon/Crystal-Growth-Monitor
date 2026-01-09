using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace Crystal_Growth_Monitor
{
    public partial class MainWindow : Window
    {
        public bool disabled = true;
        public string copiedText;

        public MainWindow()
        {
            InitializeComponent();
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
        }
    }
}
