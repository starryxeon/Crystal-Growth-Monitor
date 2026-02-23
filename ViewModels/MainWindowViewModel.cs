using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crystal_Growth_Monitor.grpc;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncUpdatable
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ViewModelBase _currentTab;

    public ObservableCollection<FurnaceViewModel> Furnaces { get; } = new();
    public MainWindowViewModel()
    {
        // Set a default tab to start
        _currentTab = new DefaultViewModel();

        // then request existing furnaces from backend
        Setup();

        // then make new furnace viewmodels
        foreach (var furnace in App.Container.states)
        {
            var f = new FurnaceViewModel(furnace.Key, furnace.Value);
            Furnaces.Add(f);
        }
        _currentTab = Furnaces[0];
    }

    [RelayCommand]
    private void TriggerPane() {
        IsPaneOpen = !IsPaneOpen;
    }

    private void Setup()
    {
        try
        {
        var response = App.GrpcClient.SendEventAsync(EventType.RequestFurnaces).GetAwaiter().GetResult();
        App.Container.Set(response);
        }
        catch(Exception ex) {Console.WriteLine(ex);}
    }

    public async void UpdateAsync(FactoryContainer container)
    {
        await Parallel.ForEachAsync(Furnaces, async (vm, vt) =>
            vm.Update(container.GetContainer(vm.FurnaceKey)));
    }
}