using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class AssemblyViewModel : ViewModelBase
{
    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private int _currentStepIndex = -1;

    [ObservableProperty]
    private AssemblyStep? _currentStep;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editNotes = string.Empty;

    public ObservableCollection<AssemblyStep> Steps { get; } = [];
    public ObservableCollection<PartCheckItem> AvailableParts { get; } = [];

    public event Action<AssemblyStep?, IList<AssemblyStep>, int>? CurrentStepChanged;

    partial void OnProjectChanged(Project? value)
    {
        Steps.Clear();
        AvailableParts.Clear();
        CurrentStepIndex = -1;
        CurrentStep = null;

        if (value == null) return;

        foreach (var step in value.AssemblySteps)
            Steps.Add(step);

        foreach (var part in value.Parts)
            AvailableParts.Add(new PartCheckItem { PartId = part.Id });
    }

    partial void OnCurrentStepIndexChanged(int value)
    {
        if (value >= 0 && value < Steps.Count)
        {
            CurrentStep = Steps[value];
            EditDescription = CurrentStep.Description;
            EditNotes = CurrentStep.Notes;

            // Update part checkboxes
            var stepPartIds = new HashSet<string>(CurrentStep.PartIds);
            foreach (var pc in AvailableParts)
                pc.IsSelected = stepPartIds.Contains(pc.PartId);

            CurrentStepChanged?.Invoke(CurrentStep, Steps, value);
        }
        else
        {
            CurrentStep = null;
            EditDescription = string.Empty;
            EditNotes = string.Empty;
            foreach (var pc in AvailableParts)
                pc.IsSelected = false;
            CurrentStepChanged?.Invoke(null, Steps, -1);
        }
    }

    [RelayCommand]
    private void AddStep()
    {
        if (Project == null) return;

        var step = new AssemblyStep
        {
            StepNumber = Steps.Count + 1,
            Description = $"Step {Steps.Count + 1}"
        };
        Steps.Add(step);
        Project.AssemblySteps.Add(step);
        CurrentStepIndex = Steps.Count - 1;
    }

    [RelayCommand]
    private void RemoveStep()
    {
        if (CurrentStep == null || Project == null) return;

        var idx = CurrentStepIndex;
        Project.AssemblySteps.Remove(CurrentStep);
        Steps.RemoveAt(idx);
        RenumberSteps();

        CurrentStepIndex = Math.Min(idx, Steps.Count - 1);
    }

    [RelayCommand]
    private void MoveStepUp()
    {
        if (CurrentStepIndex <= 0) return;
        var idx = CurrentStepIndex;
        var step = Steps[idx];
        Steps.RemoveAt(idx);
        Steps.Insert(idx - 1, step);
        if (Project != null)
        {
            Project.AssemblySteps.RemoveAt(idx);
            Project.AssemblySteps.Insert(idx - 1, step);
        }
        RenumberSteps();
        CurrentStepIndex = idx - 1;
    }

    [RelayCommand]
    private void MoveStepDown()
    {
        if (CurrentStepIndex < 0 || CurrentStepIndex >= Steps.Count - 1) return;
        var idx = CurrentStepIndex;
        var step = Steps[idx];
        Steps.RemoveAt(idx);
        Steps.Insert(idx + 1, step);
        if (Project != null)
        {
            Project.AssemblySteps.RemoveAt(idx);
            Project.AssemblySteps.Insert(idx + 1, step);
        }
        RenumberSteps();
        CurrentStepIndex = idx + 1;
    }

    [RelayCommand]
    private void SaveStepEdits()
    {
        if (CurrentStep == null) return;

        CurrentStep.Description = EditDescription;
        CurrentStep.Notes = EditNotes;
        CurrentStep.PartIds = AvailableParts
            .Where(p => p.IsSelected)
            .Select(p => p.PartId)
            .ToList();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < Steps.Count - 1)
            CurrentStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private void GenerateSteps()
    {
        if (Project == null) return;

        Steps.Clear();
        Project.AssemblySteps.Clear();

        int stepNum = 1;

        // Step 1: Preparation
        var prepStep = new AssemblyStep
        {
            StepNumber = stepNum++,
            Description = "Prepare all parts",
            Notes = "Mill lumber to final dimensions, sand to 150 grit",
            PartIds = Project.Parts.Select(p => p.Id).ToList()
        };
        Steps.Add(prepStep);
        Project.AssemblySteps.Add(prepStep);

        // One step per joint
        for (int i = 0; i < Project.Joinery.Count; i++)
        {
            var joint = Project.Joinery[i];
            var typeName = AddJointDialogViewModel.GetTypeDisplayName(joint.JoineryType);
            var step = new AssemblyStep
            {
                StepNumber = stepNum++,
                Description = $"{typeName}: {joint.PartAId} to {joint.PartBId}",
                PartIds = [joint.PartAId, joint.PartBId],
                JointIndices = [i]
            };
            Steps.Add(step);
            Project.AssemblySteps.Add(step);
        }

        // Final step: Finish
        var finishStep = new AssemblyStep
        {
            StepNumber = stepNum,
            Description = "Apply finish",
            Notes = "Sand to 220 grit, apply chosen finish",
            PartIds = Project.Parts.Select(p => p.Id).ToList()
        };
        Steps.Add(finishStep);
        Project.AssemblySteps.Add(finishStep);

        // Refresh available parts
        AvailableParts.Clear();
        foreach (var part in Project.Parts)
            AvailableParts.Add(new PartCheckItem { PartId = part.Id });

        if (Steps.Count > 0)
            CurrentStepIndex = 0;
    }

    [RelayCommand]
    private async Task ExportMarkdownAsync()
    {
        if (Steps.Count == 0 || Project == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Assembly Guide",
            DefaultExtension = "md",
            SuggestedFileName = $"{Project.Name}_assembly_guide",
            FileTypeChoices = [new FilePickerFileType("Markdown Files") { Patterns = ["*.md"] }]
        });

        if (file == null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"# {Project.Name} — Assembly Guide");
        sb.AppendLine();
        sb.AppendLine($"**Parts:** {Project.Parts.Count}  ");
        sb.AppendLine($"**Joints:** {Project.Joinery.Count}  ");
        sb.AppendLine($"**Steps:** {Steps.Count}");
        sb.AppendLine();

        foreach (var step in Steps)
        {
            sb.AppendLine($"## Step {step.StepNumber}: {step.Description}");
            sb.AppendLine();

            if (step.PartIds.Count > 0)
            {
                sb.AppendLine("**Parts involved:**");
                foreach (var id in step.PartIds)
                {
                    var part = Project.GetPart(id);
                    if (part != null)
                        sb.AppendLine($"- {part.Id} ({part.Dimensions.Length}\" x {part.Dimensions.Width}\" x {part.Dimensions.Thickness}\" {part.Material})");
                    else
                        sb.AppendLine($"- {id}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(step.Notes))
            {
                sb.AppendLine($"**Notes:** {step.Notes}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    private void RenumberSteps()
    {
        for (int i = 0; i < Steps.Count; i++)
            Steps[i].StepNumber = i + 1;
    }

    private static Window? GetMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}

public partial class PartCheckItem : ObservableObject
{
    public string PartId { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
