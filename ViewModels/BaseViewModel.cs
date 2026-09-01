using CommunityToolkit.Mvvm.ComponentModel;

namespace BalancoPatrimonial.App.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;
}