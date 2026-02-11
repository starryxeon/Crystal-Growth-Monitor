using Avalonia.Controls;
using Crystal_Growth_Monitor.ViewModels;
using Crystal_Growth_Monitor.grpc;

namespace Crystal_Growth_Monitor.Views
{
    public partial class MainWindow : Window, IAsyncUpdatable
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public async void UpdateAsync(FactoryContainer container)
        {
            
        }
    }
}
