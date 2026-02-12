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

public partial class CutListViewModel : ViewModelBase
{
    private readonly ICadService _cadService;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private CutListResult? _result;

    [ObservableProperty]
    private string? _svgPath;

    [ObservableProperty]
    private string? _svgContent;

    [ObservableProperty]
    private bool _isGenerating;

    // Stock settings
    [ObservableProperty]
    private double _stockLength = 96; // 8 feet

    [ObservableProperty]
    private double _stockWidth = 48; // 4 feet

    [ObservableProperty]
    private string _stockMaterial = "plywood";

    [ObservableProperty]
    private double _stockThickness = 0.75;

    [ObservableProperty]
    private double _kerf = 0.125;

    public ObservableCollection<StockPreset> StockPresets { get; } = [];

    public ObservableCollection<string> Materials { get; } = [];

    public CutListViewModel(ICadService cadService, IConfigService config)
    {
        _cadService = cadService;

        // Load defaults from config
        _stockLength = config.GetDouble("cutlist.stock_length", 96);
        _stockWidth = config.GetDouble("cutlist.stock_width", 48);
        _stockThickness = config.GetDouble("cutlist.stock_thickness", 0.75);
        _stockMaterial = config.GetString("cutlist.stock_material", "plywood");
        _kerf = config.GetDouble("cutlist.kerf", 0.125);

        var presets = config.GetList("cutlist.presets", row => new StockPreset(
            row.GetString("name", ""),
            row.GetDouble("length", 96),
            row.GetDouble("width", 48)
        ));
        if (presets.Count > 0)
            foreach (var p in presets) StockPresets.Add(p);
        else
        {
            StockPresets.Add(new("Full Sheet (4'\u00d78')", 96, 48));
            StockPresets.Add(new("Half Sheet (4'\u00d74')", 48, 48));
            StockPresets.Add(new("Quarter Sheet (2'\u00d74')", 48, 24));
            StockPresets.Add(new("Project Panel (2'\u00d72')", 24, 24));
        }

        var matList = config.GetStringList("cutlist.materials");
        if (matList.Count > 0)
            foreach (var m in matList) Materials.Add(m);
        else
        {
            Materials.Add("plywood");
            Materials.Add("mdf");
            Materials.Add("particle_board");
            Materials.Add("melamine");
        }
    }

    partial void OnProjectChanged(Project? value)
    {
        if (value != null && value.Parts.Count > 0)
        {
            _ = GenerateAsync().ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        else
        {
            Result = null;
        }
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (Project == null || Project.Parts.Count == 0) return;

        IsGenerating = true;
        try
        {
            // Try Python bridge first if connected
            if (_cadService.IsConnected)
            {
                Result = await _cadService.GenerateCutListAsync(
                    StockLength, StockWidth, StockMaterial, StockThickness);
            }
            else
            {
                // Local first-fit decreasing bin packing
                Result = await Task.Run(() => GenerateLocalCutList());
            }
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private CutListResult GenerateLocalCutList()
    {
        // Build list of pieces needed (expand by quantity)
        var pieces = new List<(string Id, double Length, double Width)>();
        foreach (var part in Project!.Parts)
        {
            for (var i = 0; i < part.Quantity; i++)
            {
                var suffix = part.Quantity > 1 ? $" ({i + 1})" : "";
                pieces.Add((part.Id + suffix, part.Dimensions.Length, part.Dimensions.Width));
            }
        }

        // Sort largest area first (first-fit decreasing)
        pieces = pieces.OrderByDescending(p => p.Length * p.Width).ToList();

        var sheets = new List<SheetLayout>();
        var unplaced = new List<UnplacedPiece>();

        // Available space tracking per sheet: list of (x, y, availWidth, availHeight)
        var sheetSpaces = new List<List<(double X, double Y, double W, double H)>>();

        foreach (var piece in pieces)
        {
            var pLen = piece.Length + Kerf;
            var pWid = piece.Width + Kerf;
            var placed = false;

            // Try to place in existing sheets
            for (var s = 0; s < sheets.Count && !placed; s++)
            {
                for (var r = 0; r < sheetSpaces[s].Count; r++)
                {
                    var space = sheetSpaces[s][r];

                    // Try normal orientation
                    if (pLen <= space.W + 0.001 && pWid <= space.H + 0.001)
                    {
                        sheets[s].Pieces.Add(new PlacedPiece
                        {
                            PartId = piece.Id, Label = piece.Id,
                            X = space.X, Y = space.Y,
                            Width = piece.Length, Height = piece.Width
                        });

                        // Split remaining space (guillotine cut)
                        sheetSpaces[s].RemoveAt(r);
                        var rightW = space.W - pLen;
                        var belowH = space.H - pWid;
                        if (rightW > 1)
                            sheetSpaces[s].Add((space.X + pLen, space.Y, rightW, pWid));
                        if (belowH > 1)
                            sheetSpaces[s].Add((space.X, space.Y + pWid, space.W, belowH));

                        placed = true;
                        break;
                    }

                    // Try rotated orientation
                    if (pWid <= space.W + 0.001 && pLen <= space.H + 0.001)
                    {
                        sheets[s].Pieces.Add(new PlacedPiece
                        {
                            PartId = piece.Id, Label = piece.Id,
                            X = space.X, Y = space.Y,
                            Width = piece.Width, Height = piece.Length,
                            Rotated = true
                        });

                        sheetSpaces[s].RemoveAt(r);
                        var rightW = space.W - pWid;
                        var belowH = space.H - pLen;
                        if (rightW > 1)
                            sheetSpaces[s].Add((space.X + pWid, space.Y, rightW, pLen));
                        if (belowH > 1)
                            sheetSpaces[s].Add((space.X, space.Y + pLen, space.W, belowH));

                        placed = true;
                        break;
                    }
                }
            }

            if (!placed)
            {
                // Need a new sheet?
                if (pLen <= StockLength + 0.001 && pWid <= StockWidth + 0.001)
                {
                    var newSheet = new SheetLayout
                    {
                        Stock = new StockInfo
                        {
                            Length = StockLength, Width = StockWidth,
                            Material = StockMaterial, Thickness = StockThickness
                        },
                        Pieces = [new PlacedPiece
                        {
                            PartId = piece.Id, Label = piece.Id,
                            X = 0, Y = 0,
                            Width = piece.Length, Height = piece.Width
                        }]
                    };
                    sheets.Add(newSheet);

                    var spaces = new List<(double X, double Y, double W, double H)>();
                    var rightW = StockLength - pLen;
                    var belowH = StockWidth - pWid;
                    if (rightW > 1)
                        spaces.Add((pLen, 0, rightW, pWid));
                    if (belowH > 1)
                        spaces.Add((0, pWid, StockLength, belowH));
                    sheetSpaces.Add(spaces);
                }
                else
                {
                    unplaced.Add(new UnplacedPiece
                    {
                        PartId = piece.Id,
                        Length = piece.Length,
                        Width = piece.Width
                    });
                }
            }
        }

        var totalStockArea = sheets.Count * StockLength * StockWidth;
        var totalPartsArea = pieces.Sum(p => p.Length * p.Width);
        var waste = totalStockArea > 0 ? ((totalStockArea - totalPartsArea) / totalStockArea) * 100 : 0;

        return new CutListResult
        {
            Sheets = sheets,
            Unplaced = unplaced,
            TotalStockArea = totalStockArea,
            TotalPartsArea = totalPartsArea,
            WastePercentage = waste
        };
    }

    [RelayCommand]
    private async Task ApplyPresetAsync(StockPreset preset)
    {
        StockLength = preset.Length;
        StockWidth = preset.Width;
        await GenerateAsync();
    }

    [RelayCommand]
    private async Task ExportSvgAsync()
    {
        if (Result == null || Result.Sheets.Count == 0) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Cut List as SVG",
            DefaultExtension = "svg",
            SuggestedFileName = $"{Project?.Name ?? "cutlist"}_cutlist",
            FileTypeChoices = [new FilePickerFileType("SVG Files") { Patterns = ["*.svg"] }]
        });

        if (file == null) return;

        var svg = GenerateCutListSvg();

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(svg);
    }

    private string GenerateCutListSvg()
    {
        var sb = new StringBuilder();
        const double scale = 6.0;
        const double sheetSpacing = 50;
        const double margin = 20;

        var maxSheetWidth = Result!.Sheets.Max(s => s.Stock.Length) * scale;
        var totalHeight = margin;

        foreach (var sheet in Result.Sheets)
            totalHeight += 25 + sheet.Stock.Width * scale + sheetSpacing;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{maxSheetWidth + margin * 2}\" height=\"{totalHeight}\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  text { font-family: 'Segoe UI', Arial, sans-serif; }");
        sb.AppendLine("  .sheet { fill: #F5F0E6; stroke: #555; stroke-width: 2; }");
        sb.AppendLine("  .piece { fill: #81C784; stroke: #2E7D32; stroke-width: 1; opacity: 0.9; }");
        sb.AppendLine("  .label { font-size: 9px; text-anchor: middle; dominant-baseline: central; fill: #1B5E20; }");
        sb.AppendLine("  .dim { font-size: 7px; text-anchor: middle; dominant-baseline: central; fill: #666; }");
        sb.AppendLine("  .title { font-size: 13px; font-weight: bold; fill: #333; }");
        sb.AppendLine("  .subtitle { font-size: 10px; fill: #888; }");
        sb.AppendLine("</style>");

        var yOffset = margin;

        for (var i = 0; i < Result.Sheets.Count; i++)
        {
            var sheet = Result.Sheets[i];
            var sheetW = sheet.Stock.Length * scale;
            var sheetH = sheet.Stock.Width * scale;

            sb.AppendLine($"<text x=\"{margin}\" y=\"{yOffset + 14}\" class=\"title\">Sheet {i + 1}</text>");
            sb.AppendLine($"<text x=\"{margin + 60}\" y=\"{yOffset + 14}\" class=\"subtitle\">{sheet.Stock.Length}\" x {sheet.Stock.Width}\" ({sheet.Stock.Material})</text>");
            yOffset += 25;

            // Sheet background
            sb.AppendLine($"<rect x=\"{margin}\" y=\"{yOffset}\" width=\"{sheetW}\" height=\"{sheetH}\" class=\"sheet\" rx=\"2\"/>");

            // Pieces
            foreach (var piece in sheet.Pieces)
            {
                var px = margin + piece.X * scale;
                var py = yOffset + piece.Y * scale;
                var pw = piece.Width * scale;
                var ph = piece.Height * scale;

                sb.AppendLine($"<rect x=\"{px}\" y=\"{py}\" width=\"{pw}\" height=\"{ph}\" class=\"piece\" rx=\"1\"/>");
                sb.AppendLine($"<text x=\"{px + pw / 2}\" y=\"{py + ph / 2 - 6}\" class=\"label\">{EscapeXml(piece.Label)}</text>");
                sb.AppendLine($"<text x=\"{px + pw / 2}\" y=\"{py + ph / 2 + 6}\" class=\"dim\">{piece.Width:F1}\" x {piece.Height:F1}\"</text>");
            }

            yOffset += sheetH + sheetSpacing;
        }

        // Summary
        sb.AppendLine($"<text x=\"{margin}\" y=\"{yOffset - 20}\" class=\"subtitle\">Sheets: {Result.Sheets.Count} | Waste: {Result.WastePercentage:F1}%</text>");

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}

public record StockPreset(string Name, double Length, double Width);
