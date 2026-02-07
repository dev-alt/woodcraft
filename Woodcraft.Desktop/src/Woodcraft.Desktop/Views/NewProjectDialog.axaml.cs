using Avalonia.Controls;
using Woodcraft.Desktop.ViewModels;

namespace Woodcraft.Desktop.Views;

public partial class NewProjectDialog : Window
{
    public NewProjectDialog()
    {
        InitializeComponent();
    }

    public NewProjectDialog(NewProjectDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += () => Close(viewModel.DialogResult);
    }
}
