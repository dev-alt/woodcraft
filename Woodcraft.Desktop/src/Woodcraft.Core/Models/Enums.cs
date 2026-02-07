using System.Text.Json.Serialization;

namespace Woodcraft.Core.Models;

/// <summary>
/// Unit systems supported by the application.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Units
{
    Inches,
    Millimeters,
    Centimeters,
    Feet
}

/// <summary>
/// Types of woodworking parts.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PartType
{
    Panel,
    Board,
    Rail,
    Stile,
    Shelf,
    Top,
    Bottom,
    Side,
    Back,
    DrawerFront,
    DrawerSide,
    DrawerBottom,
    Door,
    Leg,
    Apron,
    Stretcher,
    Custom
}

/// <summary>
/// Wood grain direction relative to part.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GrainDirection
{
    Length,
    Width,
    None
}

/// <summary>
/// Types of woodworking joints.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JoineryType
{
    Butt,
    Miter,
    Dado,
    Rabbet,
    Groove,
    MortiseTenon,
    ThroughMortise,
    LooseTenon,
    ThroughDovetail,
    HalfBlindDovetail,
    SlidingDovetail,
    BoxJoint,
    Biscuit,
    PocketHole,
    Dowel,
    TongueGroove
}
