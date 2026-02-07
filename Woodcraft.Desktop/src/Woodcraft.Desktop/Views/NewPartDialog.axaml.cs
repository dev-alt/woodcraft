using Avalonia.Controls;
using Woodcraft.Desktop.ViewModels;

namespace Woodcraft.Desktop.Views;

public partial class NewPartDialog : Window
{
    public NewPartDialog()
    {
        InitializeComponent();
    }

    public NewPartDialog(NewPartDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close(viewModel.DialogResult);
    }
}
