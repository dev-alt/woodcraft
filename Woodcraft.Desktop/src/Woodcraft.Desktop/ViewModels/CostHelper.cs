namespace Woodcraft.Desktop.ViewModels;

public static class CostHelper
{
    public static readonly Dictionary<string, double> CostPerBF = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pine"] = 3.50, ["poplar"] = 4.00, ["soft_maple"] = 5.00,
        ["red_oak"] = 6.50, ["white_oak"] = 7.50, ["hard_maple"] = 7.00,
        ["cherry"] = 8.50, ["walnut"] = 12.00, ["ash"] = 6.00,
        ["birch"] = 5.50, ["hickory"] = 7.00,
        ["plywood"] = 0.0, ["mdf"] = 0.0
    };

    public static readonly Dictionary<string, double> SheetCost = new(StringComparer.OrdinalIgnoreCase)
    {
        ["plywood"] = 45.00, ["mdf"] = 30.00
    };

    public static (double cost, string description) EstimateCost(
        string? material, double length, double width, double thickness, int quantity)
    {
        var mat = material ?? "pine";
        var isSheet = SheetCost.ContainsKey(mat);

        if (isSheet)
        {
            var sqft = (length * width / 144.0) * quantity;
            var sheetsNeeded = Math.Ceiling(sqft / 32.0);
            var unitCost = SheetCost.GetValueOrDefault(mat, 40.0);
            var total = sheetsNeeded * unitCost;
            return (Math.Round(total, 2), $"{sqft:F1} sq ft, {sheetsNeeded:F0} sheet(s) @ ${unitCost:F2}/sheet");
        }
        else
        {
            var bf = (length * width * thickness / 144.0) * quantity;
            var unitCost = CostPerBF.GetValueOrDefault(mat, 5.0);
            var total = bf * unitCost;
            return (Math.Round(total, 2), $"{bf:F1} bd ft @ ${unitCost:F2}/bf");
        }
    }
}
