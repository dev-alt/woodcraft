using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;
using Woodcraft.Desktop.Services;
using Woodcraft.Desktop.Views;

namespace Woodcraft.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly ICadService _cadService;
    private readonly IPythonBridge _pythonBridge;

    [ObservableProperty]
    private string _title = "Woodcraft - CAD for Woodworking";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "Ready - Create or open a project to get started";

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private Part? _selectedPart;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Child view models
    public ProjectViewModel ProjectViewModel { get; }
    public PartEditorViewModel PartEditorViewModel { get; }
    public Viewer3DViewModel Viewer3DViewModel { get; }
    public CutListViewModel CutListViewModel { get; }
    public BOMViewModel BOMViewModel { get; }
    public DrawingViewModel DrawingViewModel { get; }
    public AssemblyViewModel AssemblyViewModel { get; }

    public ObservableCollection<string> RecentFiles { get; } = [];

    public MainWindowViewModel(
        IProjectService projectService,
        ICadService cadService,
        IPythonBridge pythonBridge)
    {
        _projectService = projectService;
        _cadService = cadService;
        _pythonBridge = pythonBridge;

        // Get child view models from DI
        var services = Program.Services!;
        ProjectViewModel = services.GetRequiredService<ProjectViewModel>();
        PartEditorViewModel = services.GetRequiredService<PartEditorViewModel>();
        Viewer3DViewModel = services.GetRequiredService<Viewer3DViewModel>();
        CutListViewModel = services.GetRequiredService<CutListViewModel>();
        BOMViewModel = services.GetRequiredService<BOMViewModel>();
        DrawingViewModel = services.GetRequiredService<DrawingViewModel>();
        AssemblyViewModel = services.GetRequiredService<AssemblyViewModel>();

        // Subscribe to events
        _projectService.CurrentProjectChanged += OnProjectChanged;

        if (_pythonBridge is PythonBridge bridge)
        {
            bridge.ConnectionChanged += (_, connected) => IsConnected = connected;
        }

        // Wire up part selection: ProjectViewModel → MainWindowViewModel
        ProjectViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.SelectedPart))
            {
                SelectedPart = ProjectViewModel.SelectedPart;
            }
        };

        // Refresh tree and 3D view when part properties change
        PartEditorViewModel.ChangesApplied += () =>
        {
            if (CurrentProject != null)
            {
                ProjectViewModel.Project = CurrentProject; // Refresh tree
                Viewer3DViewModel.Project = CurrentProject; // Refresh 3D
            }
        };

        // Refresh tree when a joint is added from the 3D view
        Viewer3DViewModel.JointAdded += () =>
        {
            if (CurrentProject != null)
            {
                ProjectViewModel.Project = CurrentProject; // Refresh tree to show new joint
                PartEditorViewModel.RefreshPartJoints(); // Refresh joints in editor
            }
        };

        // Sync drag position back to editor
        Viewer3DViewModel.PartPositionChanged += part =>
        {
            if (SelectedPart?.Id == part.Id)
                PartEditorViewModel.SyncPositionFromPart(part);
        };

        // Assembly step highlighting
        AssemblyViewModel.CurrentStepChanged += (step, allSteps, index) =>
        {
            if (SelectedTabIndex == 4) // Assembly tab
                Viewer3DViewModel.HighlightAssemblyStep(step, allSteps, index);
        };

        // Set default view
        CurrentView = ProjectViewModel;
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private void OnProjectChanged(object? sender, Project? project)
    {
        CurrentProject = project;
        if (project != null)
        {
            Title = $"Woodcraft - {project.Name}";
            ProjectViewModel.Project = project;
            Viewer3DViewModel.Project = project;
            CutListViewModel.Project = project;
            BOMViewModel.Project = project;
            PartEditorViewModel.Project = project;
            AssemblyViewModel.Project = project;
            StatusMessage = $"Project '{project.Name}' loaded with {project.Parts.Count} parts";
        }
        else
        {
            Title = "Woodcraft - CAD for Woodworking";
            ProjectViewModel.Project = null;
            Viewer3DViewModel.Project = null;
            CutListViewModel.Project = null;
            BOMViewModel.Project = null;
            PartEditorViewModel.Project = null;
            AssemblyViewModel.Project = null;
            SelectedPart = null;
            StatusMessage = "Ready - Create or open a project to get started";
        }
    }

    partial void OnSelectedPartChanged(Part? value)
    {
        PartEditorViewModel.Part = value;
        DrawingViewModel.SelectedPart = value;
        Viewer3DViewModel.SelectedPart = value;

        if (value != null)
        {
            StatusMessage = $"Selected: {value.Id} ({value.Dimensions.Length}\" x {value.Dimensions.Width}\" x {value.Dimensions.Thickness}\")";
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            StatusMessage = "Connecting to CAD engine...";
            await _cadService.ConnectAsync();
            StatusMessage = "Connected to CAD engine";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var viewModel = new NewProjectDialogViewModel();
        var dialog = new NewProjectDialog(viewModel);
        var result = await dialog.ShowDialog<bool>(mainWindow);

        if (result)
        {
            var project = await _projectService.CreateProjectAsync(viewModel.ProjectName);
            project.Notes = viewModel.Description;

            // Add template parts if selected
            if (viewModel.SelectedTemplate?.DefaultParts.Length > 0)
            {
                foreach (var partName in viewModel.SelectedTemplate.DefaultParts)
                {
                    var dimensions = GetDefaultDimensionsForPart(partName, viewModel.SelectedTemplate.Id);
                    var partType = GetPartTypeFromName(partName);
                    var part = await _projectService.AddPartAsync(partName, partType, dimensions);
                    part.Material = viewModel.DefaultMaterial;
                }
                ProjectViewModel.Project = project; // Refresh
            }

            // Auto-select first part so all views populate immediately
            if (project.Parts.Count > 0)
            {
                SelectedPart = project.Parts[0];
                ProjectViewModel.SelectPartById(project.Parts[0].Id);
            }

            StatusMessage = $"Created project: {project.Name} with {project.Parts.Count} parts";
        }
    }

    private static Dimensions GetDefaultDimensionsForPart(string partName, string templateId)
    {
        // Common furniture dimensions based on part name
        return partName.ToLower() switch
        {
            var n when n.Contains("side") => new Dimensions(36, 12, 0.75),
            var n when n.Contains("top") && !n.Contains("stretcher") => new Dimensions(36, 12, 0.75),
            var n when n.Contains("bottom") => new Dimensions(34.5, 11.25, 0.75),
            var n when n.Contains("shelf") => new Dimensions(34.5, 10, 0.75),
            var n when n.Contains("back") => new Dimensions(36, 35, 0.25),
            var n when n.Contains("door") => new Dimensions(17, 35, 0.75),
            var n when n.Contains("front") && n.Contains("drawer") => new Dimensions(16, 6, 0.75),
            var n when n.Contains("front") => new Dimensions(18, 6, 0.75),
            var n when n.Contains("stretcher") => new Dimensions(30, 4, 1.5),
            var n when n.Contains("lid") => new Dimensions(14, 10, 0.75),
            _ => new Dimensions(24, 12, 0.75)
        };
    }

    private static PartType GetPartTypeFromName(string partName)
    {
        return partName.ToLower() switch
        {
            var n when n.Contains("shelf") => PartType.Shelf,
            var n when n.Contains("side") => PartType.Side,
            var n when n.Contains("top") => PartType.Top,
            var n when n.Contains("bottom") => PartType.Bottom,
            var n when n.Contains("back") => PartType.Back,
            var n when n.Contains("door") => PartType.Door,
            var n when n.Contains("drawer") && n.Contains("front") => PartType.DrawerFront,
            var n when n.Contains("drawer") && n.Contains("side") => PartType.DrawerSide,
            var n when n.Contains("drawer") && n.Contains("bottom") => PartType.DrawerBottom,
            var n when n.Contains("front") => PartType.Panel,
            var n when n.Contains("stretcher") => PartType.Stretcher,
            var n when n.Contains("rail") => PartType.Rail,
            var n when n.Contains("stile") => PartType.Stile,
            var n when n.Contains("lid") => PartType.Top,
            _ => PartType.Panel
        };
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var storageProvider = mainWindow.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Project",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Woodcraft Project") { Patterns = ["*.json"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count > 0)
        {
            try
            {
                var path = files[0].Path.LocalPath;
                await _projectService.OpenProjectAsync(path);
                StatusMessage = $"Opened: {path}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "No project to save";
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(CurrentProject.FilePath))
            {
                await SaveProjectAsAsync();
            }
            else
            {
                await _projectService.SaveProjectAsync();
                StatusMessage = "Project saved";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var storageProvider = mainWindow.StorageProvider;
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project As",
            DefaultExtension = "json",
            SuggestedFileName = CurrentProject?.Name ?? "project",
            FileTypeChoices =
            [
                new FilePickerFileType("Woodcraft Project") { Patterns = ["*.json"] }
            ]
        });

        if (file != null)
        {
            try
            {
                await _projectService.SaveProjectAsync(file.Path.LocalPath);
                StatusMessage = $"Saved: {file.Path.LocalPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task CloseProjectAsync()
    {
        if (!await ConfirmDiscardChangesAsync()) return;

        await _projectService.CloseProjectAsync();
        SelectedPart = null;
        StatusMessage = "Project closed";
    }

    [RelayCommand]
    private async Task AddPartAsync()
    {
        if (CurrentProject == null)
        {
            // If no project, create one first
            await NewProjectAsync();
            if (CurrentProject == null) return;
        }

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var viewModel = new NewPartDialogViewModel();
        viewModel.Initialize(CurrentProject.Parts.Count);

        var dialog = new NewPartDialog(viewModel);
        var result = await dialog.ShowDialog<bool>(mainWindow);

        if (result)
        {
            // Check for duplicate name
            var partName = viewModel.PartName;
            var counter = 1;
            while (CurrentProject.GetPart(partName) != null)
            {
                partName = $"{viewModel.PartName}_{counter++}";
            }

            var part = await _projectService.AddPartAsync(
                partName,
                viewModel.PartType,
                new Dimensions(viewModel.Length, viewModel.Width, viewModel.Thickness));

            part.Quantity = viewModel.Quantity;
            part.Material = viewModel.Material;
            part.GrainDirection = viewModel.GrainDirection;

            ProjectViewModel.Project = CurrentProject; // Refresh tree
            // Refresh 3D view
            Viewer3DViewModel.Project = CurrentProject;
            StatusMessage = $"Added part: {partName}";
        }
    }

    [RelayCommand]
    private async Task RemovePartAsync()
    {
        if (SelectedPart == null) return;

        var partId = SelectedPart.Id;
        if (await _projectService.RemovePartAsync(partId))
        {
            SelectedPart = null;
            ProjectViewModel.SelectedTreeItem = null;
            ProjectViewModel.Project = CurrentProject; // Refresh tree
            Viewer3DViewModel.Project = CurrentProject; // Refresh 3D
            StatusMessage = $"Removed part: {partId}";
        }
    }

    [RelayCommand]
    private async Task GenerateCutListAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "No project open";
            return;
        }

        try
        {
            StatusMessage = "Generating cut list...";
            await CutListViewModel.GenerateCommand.ExecuteAsync(null);
            SelectedTabIndex = 2; // Switch to cut list tab
            StatusMessage = "Cut list generated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cut list failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GenerateBOMAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "No project open";
            return;
        }

        try
        {
            StatusMessage = "Generating BOM...";
            await BOMViewModel.GenerateCommand.ExecuteAsync(null);
            SelectedTabIndex = 3; // Switch to BOM tab
            StatusMessage = "BOM generated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"BOM failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportStepAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "No project to export";
            return;
        }

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var storageProvider = mainWindow.StorageProvider;
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export STEP",
            DefaultExtension = "step",
            SuggestedFileName = CurrentProject.Name,
            FileTypeChoices =
            [
                new FilePickerFileType("STEP Files") { Patterns = ["*.step", "*.stp"] }
            ]
        });

        if (file != null)
        {
            if (!_cadService.IsConnected)
            {
                StatusMessage = "STEP export requires the CAD engine. Click 'Connect' in the toolbar.";
                return;
            }

            try
            {
                StatusMessage = "Exporting STEP...";
                var tempPath = await _cadService.ExportStepAsync();
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    File.Copy(tempPath, file.Path.LocalPath, overwrite: true);
                }
                StatusMessage = $"Exported: {file.Path.LocalPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ValidateDesignAsync()
    {
        if (CurrentProject == null)
        {
            StatusMessage = "No project to validate";
            return;
        }

        try
        {
            StatusMessage = "Validating design...";
            var result = await _cadService.ValidateDesignAsync();

            if (result.IsValid)
            {
                StatusMessage = "Design is valid";
            }
            else
            {
                StatusMessage = $"Found {result.Issues.Count} issues";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Validation failed: {ex.Message}";
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 4) // Assembly tab
        {
            // Re-fire current step highlighting
            if (AssemblyViewModel.CurrentStep != null)
                Viewer3DViewModel.HighlightAssemblyStep(AssemblyViewModel.CurrentStep, AssemblyViewModel.Steps, AssemblyViewModel.CurrentStepIndex);
        }
        else
        {
            Viewer3DViewModel.ClearAssemblyHighlight();
        }
    }

    [RelayCommand]
    private void GenerateAssembly()
    {
        if (CurrentProject == null) return;
        AssemblyViewModel.GenerateStepsCommand.Execute(null);
        SelectedTabIndex = 4; // Switch to Assembly tab
    }

    public bool HasUnsavedChanges => CurrentProject?.IsDirty == true;

    /// <summary>
    /// Checks for unsaved changes and prompts the user. Returns true if it's safe to close.
    /// </summary>
    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!HasUnsavedChanges) return true;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return true;

        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
        };

        var result = false;

        var panel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
        };

        panel.Children.Add(new TextBlock
        {
            Text = $"Save changes to \"{CurrentProject?.Name}\"?",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Your changes will be lost if you don't save them.",
            Opacity = 0.7,
        });

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
        };

        var discardBtn = new Button { Content = "Don't Save", Padding = new Thickness(16, 8) };
        discardBtn.Click += (_, _) => { result = true; dialog.Close(); };

        var cancelBtn = new Button { Content = "Cancel", Padding = new Thickness(16, 8) };
        cancelBtn.Click += (_, _) => { result = false; dialog.Close(); };

        var saveBtn = new Button { Content = "Save", Padding = new Thickness(16, 8), Classes = { "primary" } };
        saveBtn.Click += async (_, _) =>
        {
            await SaveProjectAsync();
            result = true;
            dialog.Close();
        };

        buttons.Children.Add(discardBtn);
        buttons.Children.Add(cancelBtn);
        buttons.Children.Add(saveBtn);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        await dialog.ShowDialog(mainWindow);

        return result;
    }

    public async Task CleanupAsync()
    {
        if (_pythonBridge is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
