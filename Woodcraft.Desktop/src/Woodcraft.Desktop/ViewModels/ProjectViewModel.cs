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
    private async Task RenamePartAsync()
    {
        var part = SelectedPart;
        if (part == null || Project == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var dialog = new Window
        {
            Title = "Rename Part",
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
        };

        var textBox = new Avalonia.Controls.TextBox { Text = part.Id, Margin = new Avalonia.Thickness(0, 8, 0, 16) };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        panel.Children.Add(new Avalonia.Controls.TextBlock
        {
            Text = "New Name:",
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
        });
        panel.Children.Add(textBox);

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
        };
        var cancelBtn = new Avalonia.Controls.Button { Content = "Cancel", Padding = new Avalonia.Thickness(16, 8) };
        cancelBtn.Click += (_, _) => dialog.Close();

        var okBtn = new Avalonia.Controls.Button { Content = "Rename", Padding = new Avalonia.Thickness(16, 8), Classes = { "primary" } };
        okBtn.Click += (_, _) =>
        {
            var newName = textBox.Text?.Trim();
            if (string.IsNullOrEmpty(newName) || newName == part.Id) { dialog.Close(); return; }
            if (Project.GetPart(newName) != null) { dialog.Close(); return; }

            var oldId = part.Id;
            part.Id = newName;

            // Update joint references
            foreach (var j in Project.Joinery)
            {
                if (j.PartAId == oldId) j.PartAId = newName;
                if (j.PartBId == oldId) j.PartBId = newName;
            }

            Project.IsDirty = true;
            RefreshTree();
            SelectPartById(newName);
            dialog.Close();
        };

        buttons.Children.Add(cancelBtn);
        buttons.Children.Add(okBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        await dialog.ShowDialog(mainWindow);
    }

    [RelayCommand]
    private void MovePartUp()
    {
        var part = SelectedPart;
        if (part == null || Project == null) return;

        var idx = Project.Parts.IndexOf(part);
        if (idx <= 0) return;

        Project.Parts.Move(idx, idx - 1);
        Project.IsDirty = true;
        RefreshTree();
        SelectPartById(part.Id);
    }

    [RelayCommand]
    private void MovePartDown()
    {
        var part = SelectedPart;
        if (part == null || Project == null) return;

        var idx = Project.Parts.IndexOf(part);
        if (idx < 0 || idx >= Project.Parts.Count - 1) return;

        Project.Parts.Move(idx, idx + 1);
        Project.IsDirty = true;
        RefreshTree();
        SelectPartById(part.Id);
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
