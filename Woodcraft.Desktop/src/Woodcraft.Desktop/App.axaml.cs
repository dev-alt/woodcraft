using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Woodcraft.Desktop.ViewModels;
using Woodcraft.Desktop.Views;

namespace Woodcraft.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = Program.Services!.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            desktop.Exit += async (_, _) =>
            {
                await viewModel.CleanupAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
