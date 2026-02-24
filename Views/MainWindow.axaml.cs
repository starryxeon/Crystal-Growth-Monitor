using Avalonia.Controls;
using Crystal_Growth_Monitor.ViewModels;
using Crystal_Growth_Monitor.grpc;
using System;
using System.Threading.Tasks;

namespace Crystal_Growth_Monitor.Views
{
    public partial class MainWindow : Window, IAsyncUpdatable
    {
        private readonly MainWindowViewModel _viewModel;
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        public async void UpdateAsync(FactoryContainer container)
        {
            _viewModel.UpdateAsync(container);
        }
    }
}
