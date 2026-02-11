using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crystal_Growth_Monitor.grpc;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncUpdatable
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentTab;

    public ObservableCollection<FurnaceViewModel> Furnaces { get; } = new()
    {
        new FurnaceViewModel("Furnace 1"),
        new FurnaceViewModel("Furnace 2"),
        new FurnaceViewModel("Furnace 3"),
        new FurnaceViewModel("Furnace 4")
    };

    public MainWindowViewModel()
    {
        // Set the first furnace as the default view on startup
        _currentTab = Furnaces[0];
    }

    [RelayCommand]
    private void TriggerPane() {
        IsPaneOpen = !IsPaneOpen;
    }

    public async void UpdateAsync(FactoryContainer container)
    {
       foreach (FurnaceViewModel furnace in Furnaces)
        {
            furnace.UpdateAsync(container.GetContainer(furnace.furnaceName));
        }
    }
}