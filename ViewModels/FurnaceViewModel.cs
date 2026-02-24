using System.Data;
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class FurnaceViewModel : ViewModelBase
{
    [ObservableProperty]
    public string furnaceName;

    [ObservableProperty]
    public Guid furnaceKey;

    [ObservableProperty]
    private double _temperature;

    [ObservableProperty]
    private FurnaceContainer? _container;

    public FurnaceViewModel(Guid key, FurnaceContainer container)
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
