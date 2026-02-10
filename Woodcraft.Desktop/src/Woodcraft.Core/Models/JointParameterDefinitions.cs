namespace Woodcraft.Core.Models;

public record JointParamDef(string DisplayName, string Key, double DefaultValue, double Increment, string Unit);

public static class JointParameterDefinitions
{
    public static IReadOnlyList<JointParamDef> GetParametersForType(JoineryType type) => type switch
    {
        JoineryType.Dado or JoineryType.Rabbet or JoineryType.Groove => new JointParamDef[]
        {
            new("Depth", "depth", 0.375, 0.0625, "in"),
            new("Width", "width", 0.75, 0.0625, "in"),
        },
        JoineryType.MortiseTenon or JoineryType.ThroughMortise or JoineryType.LooseTenon => new JointParamDef[]
        {
            new("Width", "width", 0.375, 0.0625, "in"),
            new("Height", "height", 1.5, 0.125, "in"),
            new("Depth", "depth", 1.0, 0.125, "in"),
        },
        JoineryType.ThroughDovetail or JoineryType.HalfBlindDovetail or JoineryType.SlidingDovetail => new JointParamDef[]
        {
            new("Pin Width", "pin_width", 0.25, 0.0625, "in"),
            new("Tail Width", "tail_width", 0.75, 0.0625, "in"),
            new("Angle", "angle", 14, 1, "deg"),
        },
        JoineryType.BoxJoint => new JointParamDef[]
        {
            new("Finger Width", "finger_width", 0.25, 0.0625, "in"),
            new("Finger Count", "finger_count", 8, 1, ""),
        },
        JoineryType.Biscuit => new JointParamDef[]
        {
            new("Biscuit Size", "biscuit_size", 20, 10, ""),
        },
        JoineryType.PocketHole => new JointParamDef[]
        {
            new("Screw Size", "screw_size", 1.25, 0.25, "in"),
            new("Angle", "angle", 15, 1, "deg"),
        },
        JoineryType.Dowel => new JointParamDef[]
        {
            new("Diameter", "diameter", 0.375, 0.0625, "in"),
            new("Count", "count", 2, 1, ""),
            new("Spacing", "spacing", 2.0, 0.25, "in"),
        },
        JoineryType.TongueGroove => new JointParamDef[]
        {
            new("Tongue Width", "tongue_width", 0.25, 0.0625, "in"),
            new("Tongue Length", "tongue_length", 0.375, 0.0625, "in"),
        },
        // Butt and Miter have no additional parameters
        _ => Array.Empty<JointParamDef>(),
    };
}
