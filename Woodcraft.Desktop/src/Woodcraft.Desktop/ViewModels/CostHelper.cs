using Woodcraft.Core.Interfaces;

namespace Woodcraft.Desktop.ViewModels;

public static class CostHelper
{
    public static Dictionary<string, double> CostPerBF { get; private set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pine"] = 3.50, ["poplar"] = 4.00, ["soft_maple"] = 5.00,
        ["red_oak"] = 6.50, ["white_oak"] = 7.50, ["hard_maple"] = 7.00,
        ["cherry"] = 8.50, ["walnut"] = 12.00, ["ash"] = 6.00,
        ["birch"] = 5.50, ["hickory"] = 7.00,
        ["plywood"] = 0.0, ["mdf"] = 0.0
    };

    public static Dictionary<string, double> SheetCost { get; private set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plywood"] = 45.00, ["mdf"] = 30.00
    };

    private static double _fallbackBfCost = 5.0;
    private static double _fallbackSheetCost = 40.0;

    public static void Initialize(IConfigService config)
    {
        var cfgBf = config.GetStringDoubleMap("materials.cost_per_bf");
        if (cfgBf.Count > 0)
        {
            CostPerBF = new Dictionary<string, double>(cfgBf, StringComparer.OrdinalIgnoreCase);
        }

        var cfgSheet = config.GetStringDoubleMap("materials.sheet_cost");
        if (cfgSheet.Count > 0)
        {
            SheetCost = new Dictionary<string, double>(cfgSheet, StringComparer.OrdinalIgnoreCase);
        }

        _fallbackBfCost = config.GetDouble("materials.fallback_bf_cost", _fallbackBfCost);
        _fallbackSheetCost = config.GetDouble("materials.fallback_sheet_cost", _fallbackSheetCost);
    }

    public static (double cost, string description) EstimateCost(
        string? material, double length, double width, double thickness, int quantity)
    {
        var mat = material ?? "pine";
        var isSheet = SheetCost.ContainsKey(mat);

        if (isSheet)
        {
            var sqft = (length * width / 144.0) * quantity;
            var sheetsNeeded = Math.Ceiling(sqft / 32.0);
            var unitCost = SheetCost.GetValueOrDefault(mat, _fallbackSheetCost);
            var total = sheetsNeeded * unitCost;
            return (Math.Round(total, 2), $"{sqft:F1} sq ft, {sheetsNeeded:F0} sheet(s) @ ${unitCost:F2}/sheet");
        }
        else
        {
            var bf = (length * width * thickness / 144.0) * quantity;
            var unitCost = CostPerBF.GetValueOrDefault(mat, _fallbackBfCost);
            var total = bf * unitCost;
            return (Math.Round(total, 2), $"{bf:F1} bd ft @ ${unitCost:F2}/bf");
        }
    }
}
