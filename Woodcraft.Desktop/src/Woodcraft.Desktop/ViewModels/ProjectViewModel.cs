using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;
using Woodcraft.Desktop.Views;

namespace Woodcraft.Desktop.ViewModels;

public partial class ProjectViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private PartTreeItem? _selectedTreeItem;

    [ObservableProperty]
    private Joint? _selectedJoint;

    /// <summary>
    /// The currently selected Part (derived from SelectedTreeItem).
    /// </summary>
    public Part? SelectedPart => SelectedTreeItem?.Part;

    public ObservableCollection<PartTreeItem> PartTree { get; } = [];

    public ProjectViewModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    partial void OnSelectedTreeItemChanged(PartTreeItem? value)
    {
        OnPropertyChanged(nameof(SelectedPart));
    }

    partial void OnProjectChanged(Project? value)
    {
        RefreshTree();
    }

    private void RefreshTree()
    {
        PartTree.Clear();

        if (Project == null) return;

        // Add parts category
        var partsCategory = new PartTreeItem("Parts", null);
        foreach (var part in Project.Parts)
        {
            partsCategory.Children.Add(new PartTreeItem(
                $"{part.Id} ({part.Dimensions.Length}\" × {part.Dimensions.Width}\" × {part.Dimensions.Thickness}\")",
                part));
        }
        PartTree.Add(partsCategory);

        // Add joinery category
        if (Project.Joinery.Count > 0)
        {
            var joineryCategory = new PartTreeItem("Joinery", null);
            foreach (var joint in Project.Joinery)
            {
                joineryCategory.Children.Add(new PartTreeItem(
                    $"{joint.JoineryType}: {joint.PartAId} → {joint.PartBId}",
                    null)
                { Joint = joint });
            }
            PartTree.Add(joineryCategory);
        }

        // Add hardware category
        if (Project.Hardware.Count > 0)
        {
            var hardwareCategory = new PartTreeItem("Hardware", null);
            foreach (var hw in Project.Hardware)
            {
                hardwareCategory.Children.Add(new PartTreeItem(
                    $"{hw.Name} × {hw.Quantity}",
                    null)
                { Hardware = hw });
            }
            PartTree.Add(hardwareCategory);
        }
    }

    [RelayCommand]
    private async Task AddPartAsync()
    {
        if (Project == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var viewModel = new NewPartDialogViewModel();
        viewModel.Initialize(Project.Parts.Count);

        var dialog = new NewPartDialog(viewModel);
        var result = await dialog.ShowDialog<bool>(mainWindow);

        if (result)
        {
            // Check for duplicate name
            var partName = viewModel.PartName;
            var counter = 1;
            while (Project.GetPart(partName) != null)
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

            RefreshTree();
            // Select the newly added part in the tree
            SelectPartById(part.Id);
        }
    }

    private void SelectPartById(string partId)
    {
        foreach (var category in PartTree)
        {
            foreach (var child in category.Children)
            {
                if (child.Part?.Id == partId)
                {
                    SelectedTreeItem = child;
                    return;
                }
            }
        }
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    [RelayCommand]
    private async Task RemovePartAsync()
    {
        var part = SelectedPart;
        if (part == null) return;

        await _projectService.RemovePartAsync(part.Id);
        SelectedTreeItem = null;
        RefreshTree();
    }

    [RelayCommand]
    private async Task DuplicatePartAsync()
    {
        var srcPart = SelectedPart;
        if (srcPart == null || Project == null) return;

        var baseName = srcPart.Id;
        var newName = $"{baseName}_copy";
        var counter = 1;
        while (Project.GetPart(newName) != null)
        {
            newName = $"{baseName}_copy_{counter++}";
        }

        var part = await _projectService.AddPartAsync(
            newName,
            srcPart.PartType,
            srcPart.Dimensions with { });

        part.Material = srcPart.Material;
        part.Quantity = srcPart.Quantity;
        part.GrainDirection = srcPart.GrainDirection;
        part.Notes = srcPart.Notes;

        RefreshTree();
    }
}

public partial class PartTreeItem : ObservableObject
{
    public string Name { get; }
    public Part? Part { get; }
    public Joint? Joint { get; set; }
    public Hardware? Hardware { get; set; }
    public ObservableCollection<PartTreeItem> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded = true;

    public PartTreeItem(string name, Part? part)
    {
        Name = name;
        Part = part;
    }
}
