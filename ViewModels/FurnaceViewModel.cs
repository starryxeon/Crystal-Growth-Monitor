using CommunityToolkit.Mvvm.ComponentModel;

namespace Crystal_Growth_Monitor.ViewModels;

public partial class FurnaceViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _furnaceName;

    [ObservableProperty]
    private double _temperature;

    public FurnaceViewModel(string name)
    {
        _furnaceName = name;
    }
}
