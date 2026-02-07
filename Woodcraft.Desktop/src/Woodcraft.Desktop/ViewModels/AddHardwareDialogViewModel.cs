using CommunityToolkit.Mvvm.ComponentModel;

namespace Woodcraft.Desktop.ViewModels;

public partial class AddHardwareDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private double _unitCost;

    [ObservableProperty]
    private string _supplier = string.Empty;

    public bool DialogResult { get; set; }
    public Action? CloseRequested { get; set; }
}
