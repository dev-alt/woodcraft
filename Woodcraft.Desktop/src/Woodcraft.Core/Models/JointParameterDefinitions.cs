using Woodcraft.Core.Interfaces;

namespace Woodcraft.Core.Models;

public record JointParamDef(string DisplayName, string Key, double DefaultValue, double Increment, string Unit);

public static class JointParameterDefinitions
{
    // Mutable overrides loaded from config.lua; keyed by JoineryType name
    private static Dictionary<string, JointParamDef[]>? _overrides;

    public static void Initialize(IConfigService config)
    {
        _overrides = new Dictionary<string, JointParamDef[]>(StringComparer.Ordinal);

        foreach (var joineryType in Enum.GetValues<JoineryType>())
        {
            var typeName = joineryType.ToString();
            var path = $"joints.{typeName}";
            var list = config.GetList(path, row => new JointParamDef(
                row.GetString("display", ""),
                row.GetString("key", ""),
                row.GetDouble("default_value", 0),
                row.GetDouble("increment", 0.0625),
                row.GetString("unit", "in")
            ));

            if (list.Count > 0)
                _overrides[typeName] = list.ToArray();
        }
    }

    public static IReadOnlyList<JointParamDef> GetParametersForType(JoineryType type)
    {
        // Check config overrides first
        if (_overrides != null && _overrides.TryGetValue(type.ToString(), out var overridden))
            return overridden;

        // Hardcoded fallbacks
        return type switch
        {
            JoineryType.Dado or JoineryType.Rabbet or JoineryType.Groove =>
            [
                new("Depth", "depth", 0.375, 0.0625, "in"),
                new("Width", "width", 0.75, 0.0625, "in"),
            ],
            JoineryType.MortiseTenon or JoineryType.ThroughMortise or JoineryType.LooseTenon =>
            [
                new("Width", "width", 0.375, 0.0625, "in"),
                new("Height", "height", 1.5, 0.125, "in"),
                new("Depth", "depth", 1.0, 0.125, "in"),
            ],
            JoineryType.ThroughDovetail or JoineryType.HalfBlindDovetail or JoineryType.SlidingDovetail =>
            [
                new("Pin Width", "pin_width", 0.25, 0.0625, "in"),
                new("Tail Width", "tail_width", 0.75, 0.0625, "in"),
                new("Angle", "angle", 14, 1, "deg"),
            ],
            JoineryType.BoxJoint =>
            [
                new("Finger Width", "finger_width", 0.25, 0.0625, "in"),
                new("Finger Count", "finger_count", 8, 1, ""),
            ],
            JoineryType.Biscuit =>
            [
                new("Biscuit Size", "biscuit_size", 20, 10, ""),
            ],
            JoineryType.PocketHole =>
            [
                new("Screw Size", "screw_size", 1.25, 0.25, "in"),
                new("Angle", "angle", 15, 1, "deg"),
            ],
            JoineryType.Dowel =>
            [
                new("Diameter", "diameter", 0.375, 0.0625, "in"),
                new("Count", "count", 2, 1, ""),
                new("Spacing", "spacing", 2.0, 0.25, "in"),
            ],
            JoineryType.TongueGroove =>
            [
                new("Tongue Width", "tongue_width", 0.25, 0.0625, "in"),
                new("Tongue Length", "tongue_length", 0.375, 0.0625, "in"),
            ],
            _ => Array.Empty<JointParamDef>(),
        };
    }
}
