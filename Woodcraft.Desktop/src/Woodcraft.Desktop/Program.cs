using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Woodcraft.Core.Interfaces;
using Woodcraft.Desktop.Services;
using Woodcraft.Desktop.ViewModels;

namespace Woodcraft.Desktop;

public static class Program
{
    public static IServiceProvider? Services { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        // Build service provider
        Services = ConfigureServices();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Services
        services.AddSingleton<IPythonBridge, PythonBridge>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ICadService, CadService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<ProjectViewModel>();
        services.AddTransient<PartEditorViewModel>();
        services.AddTransient<Viewer3DViewModel>();
        services.AddTransient<CutListViewModel>();
        services.AddTransient<BOMViewModel>();
        services.AddTransient<DrawingViewModel>();

        return services.BuildServiceProvider();
    }
}
