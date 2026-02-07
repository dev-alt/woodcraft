using System.Collections.ObjectModel;
using System.Globalization;
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

public partial class DrawingViewModel : ViewModelBase
{
    private readonly ICadService _cadService;

    [ObservableProperty]
    private Part? _selectedPart;

    [ObservableProperty]
    private string? _drawingPath;

    [ObservableProperty]
    private string? _drawingContent;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _outputFormat = "svg";

    [ObservableProperty]
    private double _scale = 1.0;

    // View selections
    [ObservableProperty]
    private bool _showTopView = true;

    [ObservableProperty]
    private bool _showFrontView = true;

    [ObservableProperty]
    private bool _showSideView = true;

    [ObservableProperty]
    private bool _showDimensions = true;

    public ObservableCollection<string> OutputFormats { get; } = ["svg", "dxf"];

    public ObservableCollection<double> Scales { get; } = [0.25, 0.5, 0.75, 1.0, 1.5, 2.0];

    public DrawingViewModel(ICadService cadService)
    {
        _cadService = cadService;
    }

    partial void OnSelectedPartChanged(Part? value)
    {
        if (value != null)
        {
            _ = GenerateAsync();
        }
        else
        {
            DrawingContent = null;
            DrawingPath = null;
        }
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (SelectedPart == null) return;

        IsGenerating = true;
        try
        {
            if (_cadService.IsConnected)
            {
                var views = new List<string>();
                if (ShowTopView) views.Add("top");
                if (ShowFrontView) views.Add("front");
                if (ShowSideView) views.Add("side");

                DrawingPath = await _cadService.GenerateDrawingAsync(
                    SelectedPart.Id,
                    views.ToArray(),
                    OutputFormat);

                if (!string.IsNullOrEmpty(DrawingPath) && File.Exists(DrawingPath))
                {
                    DrawingContent = await File.ReadAllTextAsync(DrawingPath);
                }
            }
            else
            {
                // Local generation - the XAML view renders via bindings,
                // but we also generate SVG content for export
                DrawingContent = GenerateLocalSvg();
            }
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task ExportSvgAsync()
    {
        if (SelectedPart == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Drawing as SVG",
            DefaultExtension = "svg",
            SuggestedFileName = $"{SelectedPart.Id}_drawing",
            FileTypeChoices = [new FilePickerFileType("SVG Files") { Patterns = ["*.svg"] }]
        });

        if (file == null) return;

        var svg = GenerateLocalSvg();

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(svg);
    }

    [RelayCommand]
    private async Task ExportDxfAsync()
    {
        if (SelectedPart == null) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Drawing as DXF",
            DefaultExtension = "dxf",
            SuggestedFileName = $"{SelectedPart.Id}_drawing",
            FileTypeChoices = [new FilePickerFileType("DXF Files") { Patterns = ["*.dxf"] }]
        });

        if (file == null) return;

        var dxf = GenerateLocalDxf();

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(dxf);
    }

    private string GenerateLocalSvg()
    {
        var part = SelectedPart!;
        var len = part.Dimensions.Length * Scale;
        var wid = part.Dimensions.Width * Scale;
        var thk = part.Dimensions.Thickness * Scale;
        const double viewScale = 4.0; // pixels per inch for display

        var sb = new StringBuilder();

        // Calculate layout
        var views = new List<(string Title, double W, double H, double RealW, double RealH)>();
        if (ShowTopView) views.Add(("TOP VIEW", len * viewScale, wid * viewScale, part.Dimensions.Length, part.Dimensions.Width));
        if (ShowFrontView) views.Add(("FRONT VIEW", len * viewScale, thk * viewScale, part.Dimensions.Length, part.Dimensions.Thickness));
        if (ShowSideView) views.Add(("SIDE VIEW", wid * viewScale, thk * viewScale, part.Dimensions.Width, part.Dimensions.Thickness));

        if (views.Count == 0) views.Add(("TOP VIEW", len * viewScale, wid * viewScale, part.Dimensions.Length, part.Dimensions.Width));

        const double margin = 60;
        const double spacing = 80;
        const double titleBlockH = 60;

        // Arrange views in a row
        var totalW = views.Sum(v => v.W) + spacing * (views.Count - 1) + margin * 2;
        var maxH = views.Max(v => v.H);
        var totalH = maxH + margin * 2 + titleBlockH + 60; // extra for labels

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{totalW}\" height=\"{totalH}\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  text { font-family: 'Consolas', 'Courier New', monospace; }");
        sb.AppendLine("  .view-rect { fill: none; stroke: #333; stroke-width: 2; }");
        sb.AppendLine("  .dim-line { stroke: #666; stroke-width: 0.5; }");
        sb.AppendLine("  .dim-text { font-size: 10px; fill: #333; text-anchor: middle; }");
        sb.AppendLine("  .view-title { font-size: 11px; font-weight: bold; fill: #D4894B; text-anchor: middle; }");
        sb.AppendLine("  .title-block { fill: #f8f8f8; stroke: #333; stroke-width: 2; }");
        sb.AppendLine("  .title-text { font-size: 14px; font-weight: bold; fill: #333; }");
        sb.AppendLine("  .info-text { font-size: 10px; fill: #666; }");
        sb.AppendLine("</style>");

        // Background
        sb.AppendLine($"<rect width=\"{totalW}\" height=\"{totalH}\" fill=\"white\"/>");

        // Border
        sb.AppendLine($"<rect x=\"10\" y=\"10\" width=\"{totalW - 20}\" height=\"{totalH - 20}\" fill=\"none\" stroke=\"#333\" stroke-width=\"2\"/>");

        // Title block
        var tbY = totalH - titleBlockH - 10;
        sb.AppendLine($"<rect x=\"10\" y=\"{tbY}\" width=\"{totalW - 20}\" height=\"{titleBlockH}\" class=\"title-block\"/>");
        sb.AppendLine($"<text x=\"30\" y=\"{tbY + 22}\" class=\"title-text\">{EscapeXml(part.Id)}</text>");
        sb.AppendLine($"<text x=\"30\" y=\"{tbY + 40}\" class=\"info-text\">Dimensions: {part.Dimensions.Length}\" x {part.Dimensions.Width}\" x {part.Dimensions.Thickness}\"</text>");
        sb.AppendLine($"<text x=\"{totalW - 150}\" y=\"{tbY + 22}\" class=\"info-text\">Scale: {Scale}:1</text>");
        sb.AppendLine($"<text x=\"{totalW - 150}\" y=\"{tbY + 40}\" class=\"info-text\">Material: {part.Material ?? "N/A"}</text>");
        sb.AppendLine($"<text x=\"{totalW / 2}\" y=\"{tbY + 50}\" class=\"info-text\" text-anchor=\"middle\">WOODCRAFT</text>");

        // Draw views
        var xPos = margin;
        var yCenter = margin + maxH / 2;

        foreach (var (title, w, h, realW, realH) in views)
        {
            var vx = xPos;
            var vy = yCenter - h / 2;

            // View title
            sb.AppendLine($"<text x=\"{vx + w / 2}\" y=\"{vy - 12}\" class=\"view-title\">{title}</text>");

            // View rectangle
            sb.AppendLine($"<rect x=\"{vx}\" y=\"{vy}\" width=\"{w}\" height=\"{h}\" class=\"view-rect\"/>");

            if (ShowDimensions)
            {
                // Width dimension (bottom)
                var dimY = vy + h + 20;
                sb.AppendLine($"<line x1=\"{vx}\" y1=\"{dimY - 5}\" x2=\"{vx}\" y2=\"{dimY + 5}\" class=\"dim-line\"/>");
                sb.AppendLine($"<line x1=\"{vx + w}\" y1=\"{dimY - 5}\" x2=\"{vx + w}\" y2=\"{dimY + 5}\" class=\"dim-line\"/>");
                sb.AppendLine($"<line x1=\"{vx}\" y1=\"{dimY}\" x2=\"{vx + w}\" y2=\"{dimY}\" class=\"dim-line\"/>");
                sb.AppendLine($"<text x=\"{vx + w / 2}\" y=\"{dimY + 15}\" class=\"dim-text\">{realW}\"</text>");

                // Height dimension (right)
                var dimX = vx + w + 20;
                sb.AppendLine($"<line x1=\"{dimX - 5}\" y1=\"{vy}\" x2=\"{dimX + 5}\" y2=\"{vy}\" class=\"dim-line\"/>");
                sb.AppendLine($"<line x1=\"{dimX - 5}\" y1=\"{vy + h}\" x2=\"{dimX + 5}\" y2=\"{vy + h}\" class=\"dim-line\"/>");
                sb.AppendLine($"<line x1=\"{dimX}\" y1=\"{vy}\" x2=\"{dimX}\" y2=\"{vy + h}\" class=\"dim-line\"/>");
                sb.AppendLine($"<text x=\"{dimX + 15}\" y=\"{vy + h / 2 + 4}\" class=\"dim-text\">{realH}\"</text>");
            }

            xPos += w + spacing;
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private string GenerateLocalDxf()
    {
        var part = SelectedPart!;
        var sb = new StringBuilder();

        // DXF header
        sb.AppendLine("0\nSECTION\n2\nHEADER\n0\nENDSEC");

        // Entities section
        sb.AppendLine("0\nSECTION\n2\nENTITIES");

        double xOffset = 0;
        const double viewGap = 20;

        // Top view (Length x Width)
        if (ShowTopView)
        {
            AppendDxfRect(sb, xOffset, 0, part.Dimensions.Length, part.Dimensions.Width, "TOP");
            if (ShowDimensions)
            {
                AppendDxfText(sb, xOffset + part.Dimensions.Length / 2, -3,
                    $"{part.Dimensions.Length}\"", 2);
                AppendDxfText(sb, xOffset + part.Dimensions.Length + 3, part.Dimensions.Width / 2,
                    $"{part.Dimensions.Width}\"", 2);
            }
            xOffset += part.Dimensions.Length + viewGap;
        }

        // Front view (Length x Thickness)
        if (ShowFrontView)
        {
            AppendDxfRect(sb, xOffset, 0, part.Dimensions.Length, part.Dimensions.Thickness, "FRONT");
            if (ShowDimensions)
            {
                AppendDxfText(sb, xOffset + part.Dimensions.Length / 2, -3,
                    $"{part.Dimensions.Length}\"", 2);
                AppendDxfText(sb, xOffset + part.Dimensions.Length + 3, part.Dimensions.Thickness / 2,
                    $"{part.Dimensions.Thickness}\"", 2);
            }
            xOffset += part.Dimensions.Length + viewGap;
        }

        // Side view (Width x Thickness)
        if (ShowSideView)
        {
            AppendDxfRect(sb, xOffset, 0, part.Dimensions.Width, part.Dimensions.Thickness, "SIDE");
            if (ShowDimensions)
            {
                AppendDxfText(sb, xOffset + part.Dimensions.Width / 2, -3,
                    $"{part.Dimensions.Width}\"", 2);
                AppendDxfText(sb, xOffset + part.Dimensions.Width + 3, part.Dimensions.Thickness / 2,
                    $"{part.Dimensions.Thickness}\"", 2);
            }
        }

        sb.AppendLine("0\nENDSEC\n0\nEOF");
        return sb.ToString();
    }

    private static void AppendDxfRect(StringBuilder sb, double x, double y, double w, double h, string layer)
    {
        var ci = CultureInfo.InvariantCulture;
        // Bottom line
        AppendDxfLine(sb, x, y, x + w, y, layer);
        // Right line
        AppendDxfLine(sb, x + w, y, x + w, y + h, layer);
        // Top line
        AppendDxfLine(sb, x + w, y + h, x, y + h, layer);
        // Left line
        AppendDxfLine(sb, x, y + h, x, y, layer);
    }

    private static void AppendDxfLine(StringBuilder sb, double x1, double y1, double x2, double y2, string layer)
    {
        var ci = CultureInfo.InvariantCulture;
        sb.AppendLine("0\nLINE");
        sb.AppendLine($"8\n{layer}");
        sb.AppendLine($"10\n{x1.ToString(ci)}");
        sb.AppendLine($"20\n{y1.ToString(ci)}");
        sb.AppendLine("30\n0.0");
        sb.AppendLine($"11\n{x2.ToString(ci)}");
        sb.AppendLine($"21\n{y2.ToString(ci)}");
        sb.AppendLine("31\n0.0");
    }

    private static void AppendDxfText(StringBuilder sb, double x, double y, string text, double height)
    {
        var ci = CultureInfo.InvariantCulture;
        sb.AppendLine("0\nTEXT");
        sb.AppendLine("8\nDIMENSIONS");
        sb.AppendLine($"10\n{x.ToString(ci)}");
        sb.AppendLine($"20\n{y.ToString(ci)}");
        sb.AppendLine("30\n0.0");
        sb.AppendLine($"40\n{height.ToString(ci)}");
        sb.AppendLine($"1\n{text}");
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
