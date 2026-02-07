using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class BOMViewModel : ViewModelBase
{
    private readonly ICadService _cadService;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private BillOfMaterials? _bom;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private double _totalCost;

    [ObservableProperty]
    private string _outputFormat = "json";

    public ObservableCollection<BOMLineItem> LineItems { get; } = [];

    public ObservableCollection<string> OutputFormats { get; } = ["json", "csv", "text"];

    public BOMViewModel(ICadService cadService)
    {
        _cadService = cadService;
    }

    [RelayCommand]
    public async Task GenerateAsync()
    {
        if (Project == null || Project.Parts.Count == 0) return;

        IsGenerating = true;
        try
        {
            if (_cadService.IsConnected)
            {
                Bom = await _cadService.GenerateBOMAsync();
            }
            else
            {
                Bom = await Task.Run(() => GenerateLocalBOM());
            }

            TotalCost = Bom.TotalCost;

            // Flatten for display
            LineItems.Clear();
            foreach (var category in Bom.Categories)
            {
                LineItems.Add(new BOMLineItem
                {
                    IsCategory = true,
                    Name = category.Name,
                    Subtotal = category.Subtotal
                });

                foreach (var item in category.Items)
                {
                    LineItems.Add(new BOMLineItem
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitCost = item.UnitCost,
                        TotalCost = item.TotalCost,
                        Supplier = item.Supplier,
                        Notes = item.Notes
                    });
                }
            }
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private BillOfMaterials GenerateLocalBOM()
    {
        // Rough lumber cost per board foot by species
        var costPerBF = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["pine"] = 3.50, ["poplar"] = 4.00, ["soft_maple"] = 5.00,
            ["red_oak"] = 6.50, ["white_oak"] = 7.50, ["hard_maple"] = 7.00,
            ["cherry"] = 8.50, ["walnut"] = 12.00, ["ash"] = 6.00,
            ["birch"] = 5.50, ["hickory"] = 7.00,
            ["plywood"] = 0.0, ["mdf"] = 0.0 // priced per sheet
        };

        var sheetCost = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["plywood"] = 45.00, ["mdf"] = 30.00
        };

        // Group parts by material
        var byMaterial = Project!.Parts
            .GroupBy(p => p.Material ?? "pine")
            .OrderBy(g => g.Key);

        var lumberItems = new List<BOMItem>();
        var sheetItems = new List<BOMItem>();
        double lumberSubtotal = 0;
        double sheetSubtotal = 0;

        foreach (var group in byMaterial)
        {
            var material = group.Key;
            var isSheet = sheetCost.ContainsKey(material);

            foreach (var part in group)
            {
                var bf = part.Dimensions.BoardFeet * part.Quantity;
                var sqft = part.Dimensions.SquareFeet * part.Quantity;
                double unitCost, total;
                string unit, desc;

                if (isSheet)
                {
                    // Sheet goods: price per 4x8 sheet (32 sq ft)
                    var sheetsNeeded = Math.Ceiling(sqft / 32.0);
                    unitCost = sheetCost.GetValueOrDefault(material, 40.0);
                    total = sheetsNeeded * unitCost;
                    unit = "sheet";
                    desc = $"{part.Dimensions} - {material}";
                    sheetSubtotal += total;

                    sheetItems.Add(new BOMItem
                    {
                        Name = part.Id,
                        Description = desc,
                        Quantity = part.Quantity,
                        Unit = unit,
                        UnitCost = unitCost,
                        TotalCost = total,
                        Notes = $"{sqft:F2} sq ft"
                    });
                }
                else
                {
                    unitCost = costPerBF.GetValueOrDefault(material, 5.0);
                    total = bf * unitCost;
                    unit = "bd ft";
                    desc = $"{part.Dimensions} - {material}";
                    lumberSubtotal += total;

                    lumberItems.Add(new BOMItem
                    {
                        Name = part.Id,
                        Description = desc,
                        Quantity = Math.Round(bf, 2),
                        Unit = unit,
                        UnitCost = unitCost,
                        TotalCost = Math.Round(total, 2),
                        Notes = $"Qty: {part.Quantity}"
                    });
                }
            }
        }

        // Hardware
        var hardwareItems = Project.Hardware.Select(h => new BOMItem
        {
            Name = h.Name,
            Description = h.Description,
            Quantity = h.Quantity,
            Unit = "each",
            UnitCost = h.UnitCost,
            TotalCost = h.TotalCost
        }).ToList();
        var hardwareSubtotal = hardwareItems.Sum(i => i.TotalCost);

        var categories = new List<BOMCategory>();
        if (lumberItems.Count > 0)
            categories.Add(new BOMCategory { Name = "Lumber", Items = lumberItems, Subtotal = Math.Round(lumberSubtotal, 2) });
        if (sheetItems.Count > 0)
            categories.Add(new BOMCategory { Name = "Sheet Goods", Items = sheetItems, Subtotal = Math.Round(sheetSubtotal, 2) });
        if (hardwareItems.Count > 0)
            categories.Add(new BOMCategory { Name = "Hardware", Items = hardwareItems, Subtotal = Math.Round(hardwareSubtotal, 2) });

        return new BillOfMaterials
        {
            ProjectName = Project.Name,
            Categories = categories,
            TotalCost = Math.Round(lumberSubtotal + sheetSubtotal + hardwareSubtotal, 2)
        };
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (Bom == null || LineItems.Count == 0) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export BOM as CSV",
            DefaultExtension = "csv",
            SuggestedFileName = $"{Bom.ProjectName}_bom",
            FileTypeChoices = [new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }]
        });

        if (file == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Category,Item,Description,Quantity,Unit,Unit Cost,Total Cost,Supplier,Notes");

        var currentCategory = "";
        foreach (var item in LineItems)
        {
            if (item.IsCategory)
            {
                currentCategory = item.Name;
                continue;
            }

            sb.AppendLine($"\"{currentCategory}\",\"{item.Name}\",\"{item.Description}\",{item.Quantity},\"{item.Unit}\",{item.UnitCost:F2},{item.TotalCost:F2},\"{item.Supplier}\",\"{item.Notes}\"");
        }

        sb.AppendLine();
        sb.AppendLine($",,,,,,{TotalCost:F2},,Total");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    [RelayCommand]
    private async Task AddHardwareAsync()
    {
        if (Project == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var vm = new AddHardwareDialogViewModel();
        var dialog = new Window
        {
            Title = "Add Hardware",
            Width = 400, Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            DataContext = vm,
            Content = BuildAddHardwareContent(vm)
        };

        vm.CloseRequested += () => dialog.Close(vm.DialogResult);
        var result = await dialog.ShowDialog<bool>(mainWindow);

        if (result)
        {
            var hw = new Hardware(vm.Name, vm.Quantity, vm.UnitCost)
            {
                Description = vm.Description,
                Supplier = vm.Supplier
            };
            Project.Hardware.Add(hw);

            // Regenerate BOM to include the new hardware
            await GenerateAsync();
        }
    }

    private static Panel BuildAddHardwareContent(AddHardwareDialogViewModel vm)
    {
        var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12 };

        stack.Children.Add(new TextBlock { Text = "Add Hardware Item", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold });

        AddField(stack, "Name", vm, nameof(vm.Name));
        AddField(stack, "Description", vm, nameof(vm.Description));
        AddNumericField(stack, "Quantity", vm, nameof(vm.Quantity));
        AddNumericField(stack, "Unit Cost ($)", vm, nameof(vm.UnitCost));
        AddField(stack, "Supplier", vm, nameof(vm.Supplier));

        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 12, 0, 0) };
        var cancelBtn = new Button { Content = "Cancel" };
        cancelBtn.Click += (_, _) => { vm.DialogResult = false; vm.CloseRequested?.Invoke(); };
        var addBtn = new Button { Content = "Add", Classes = { "primary" } };
        addBtn.Click += (_, _) => { vm.DialogResult = true; vm.CloseRequested?.Invoke(); };
        buttons.Children.Add(cancelBtn);
        buttons.Children.Add(addBtn);
        stack.Children.Add(buttons);

        return stack;
    }

    private static void AddField(StackPanel parent, string label, object dc, string path)
    {
        parent.Children.Add(new TextBlock { Text = label, FontSize = 11, Opacity = 0.7 });
        var tb = new TextBox { [!TextBox.TextProperty] = new Avalonia.Data.Binding(path) { Source = dc } };
        parent.Children.Add(tb);
    }

    private static void AddNumericField(StackPanel parent, string label, object dc, string path)
    {
        parent.Children.Add(new TextBlock { Text = label, FontSize = 11, Opacity = 0.7 });
        var nud = new NumericUpDown { Minimum = 0, Increment = 1, FormatString = "F2", [!NumericUpDown.ValueProperty] = new Avalonia.Data.Binding(path) { Source = dc } };
        parent.Children.Add(nud);
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}

public partial class BOMLineItem : ObservableObject
{
    [ObservableProperty]
    private bool _isCategory;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private double _quantity;

    [ObservableProperty]
    private string _unit = "each";

    [ObservableProperty]
    private double _unitCost;

    [ObservableProperty]
    private double _totalCost;

    [ObservableProperty]
    private double _subtotal;

    [ObservableProperty]
    private string _supplier = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;
}
