using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;
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

        // Initialize static helpers that read from config
        var config = Services.GetRequiredService<IConfigService>();
        CostHelper.Initialize(config);
        MaterialInfo.Initialize(config);
        JointParameterDefinitions.Initialize(config);

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
        services.AddSingleton<IConfigService, LuaConfigService>();
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
        services.AddTransient<AssemblyViewModel>();

        return services.BuildServiceProvider();
    }
}
