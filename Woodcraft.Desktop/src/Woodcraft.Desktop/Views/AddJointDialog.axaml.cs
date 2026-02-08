using Avalonia.Controls;
using Woodcraft.Desktop.ViewModels;

namespace Woodcraft.Desktop.Views;

public partial class AddJointDialog : Window
{
    public AddJointDialog()
    {
        InitializeComponent();
    }

    public AddJointDialog(AddJointDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close(viewModel.DialogResult);
    }
}
