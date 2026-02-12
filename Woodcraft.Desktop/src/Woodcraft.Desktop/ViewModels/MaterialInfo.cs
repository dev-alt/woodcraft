using Avalonia;
using Avalonia.Media;
using Woodcraft.Core.Interfaces;

namespace Woodcraft.Desktop.ViewModels;

public record MaterialInfo(string Id, string DisplayName, string PriceCategory, Color WoodColor)
{
    public IBrush SwatchBrush => new SolidColorBrush(WoodColor);

    private static readonly List<MaterialInfo> _defaults =
    [
        new("pine", "Pine", "$", Color.Parse("#F5DEB3")),
        new("red_oak", "Red Oak", "$$", Color.Parse("#C4956A")),
        new("white_oak", "White Oak", "$$", Color.Parse("#D4A76A")),
        new("hard_maple", "Hard Maple", "$$", Color.Parse("#E8D5B7")),
        new("soft_maple", "Soft Maple", "$$", Color.Parse("#DEC9A4")),
        new("cherry", "Cherry", "$$$", Color.Parse("#B5651D")),
        new("walnut", "Walnut", "$$$", Color.Parse("#5C4033")),
        new("poplar", "Poplar", "$", Color.Parse("#C9B99A")),
        new("ash", "Ash", "$$", Color.Parse("#D2C6A5")),
        new("birch", "Birch", "$$", Color.Parse("#E6D5B8")),
        new("hickory", "Hickory", "$$", Color.Parse("#C49A6C")),
        new("plywood", "Plywood", "$", Color.Parse("#D2B48C")),
        new("mdf", "MDF", "$", Color.Parse("#B8956A")),
    ];

    public static IReadOnlyList<MaterialInfo> All { get; private set; } = _defaults;

    // Color offsets for wood grain gradient
    private static int _lighterR = 25, _lighterG = 20, _lighterB = 15;
    private static int _darkerR = 30, _darkerG = 25, _darkerB = 20;

    public static void Initialize(IConfigService config)
    {
        var catalog = config.GetList("materials.catalog", row => new MaterialInfo(
            row.GetString("id", ""),
            row.GetString("display", ""),
            row.GetString("price", "$"),
            Color.Parse(row.GetString("color", "#C0C0C0"))
        ));

        if (catalog.Count > 0)
            All = catalog;

        var lighter = config.GetTable("materials.color_lighter");
        if (lighter != null)
        {
            _lighterR = lighter.GetInt("r", _lighterR);
            _lighterG = lighter.GetInt("g", _lighterG);
            _lighterB = lighter.GetInt("b", _lighterB);
        }

        var darker = config.GetTable("materials.color_darker");
        if (darker != null)
        {
            _darkerR = darker.GetInt("r", _darkerR);
            _darkerG = darker.GetInt("g", _darkerG);
            _darkerB = darker.GetInt("b", _darkerB);
        }
    }

    public static MaterialInfo GetById(string? id) =>
        All.FirstOrDefault(m => m.Id == id) ?? All[0];

    public static IBrush CreateWoodBrush(string? materialId)
    {
        var info = GetById(materialId);
        var baseColor = info.WoodColor;
        var lighter = Color.FromRgb(
            (byte)Math.Min(255, baseColor.R + _lighterR),
            (byte)Math.Min(255, baseColor.G + _lighterG),
            (byte)Math.Min(255, baseColor.B + _lighterB));
        var darker = Color.FromRgb(
            (byte)Math.Max(0, baseColor.R - _darkerR),
            (byte)Math.Max(0, baseColor.G - _darkerG),
            (byte)Math.Max(0, baseColor.B - _darkerB));

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        };
        brush.GradientStops.Add(new GradientStop(darker, 0));
        brush.GradientStops.Add(new GradientStop(lighter, 0.25));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.5));
        brush.GradientStops.Add(new GradientStop(lighter, 0.75));
        brush.GradientStops.Add(new GradientStop(darker, 1));
        return brush;
    }

    public static IBrush CreateBorderBrush(string? materialId)
    {
        var info = GetById(materialId);
        var baseColor = info.WoodColor;
        return new SolidColorBrush(Color.FromRgb(
            (byte)Math.Max(0, baseColor.R - 60),
            (byte)Math.Max(0, baseColor.G - 50),
            (byte)Math.Max(0, baseColor.B - 40)));
    }
}
