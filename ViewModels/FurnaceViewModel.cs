using System.Data;
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class FurnaceViewModel : ViewModelBase
{
    [ObservableProperty]
    public string furnaceName;

    [ObservableProperty]
    public int furnaceKey;  // hash key to be generated when creating a new furnace, or provided by the backend for existing furnaces

    [ObservableProperty]
    private double _temperature;

    [ObservableProperty]
    private FurnaceContainer? _container;

    public FurnaceViewModel(int key, FurnaceContainer container)
    {
        furnaceKey = key;
        _container = Container;
        furnaceName = container.label;
    }
    
    public void Update(FurnaceContainer newContainer)
    {
        Container = newContainer;
    }
}
