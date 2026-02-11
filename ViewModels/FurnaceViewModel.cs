using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class FurnaceViewModel : ViewModelBase
{
    [ObservableProperty]
    public string furnaceName;

    [ObservableProperty]
    private double _temperature;

    [ObservableProperty]
    private FurnaceContainer? _container;

    public FurnaceViewModel(string name)
    {
        furnaceName = name;
    }
    
    public async void UpdateAsync(FurnaceContainer newContainer)
    {
        Container = newContainer;
    }
}
