using Avalonia.Controls;
using Crystal_Growth_Monitor.ViewModels;

namespace Crystal_Growth_Monitor.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
