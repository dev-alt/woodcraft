using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.ViewModels;

public partial class AddJointDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _partAName = string.Empty;

    [ObservableProperty]
    private string _partBName = string.Empty;

    [ObservableProperty]
    private JoineryType _selectedType = JoineryType.Butt;

    [ObservableProperty]
    private string _description = "Simple end-to-end joint, good for face frames and basic construction.";

    public bool DialogResult { get; set; }
    public Action? CloseRequested { get; set; }

    public ObservableCollection<JoineryType> JoineryTypes { get; } =
        new(Enum.GetValues<JoineryType>());

    private static readonly Dictionary<JoineryType, string> Descriptions = new()
    {
        [JoineryType.Butt] = "Simple end-to-end joint, good for face frames and basic construction.",
        [JoineryType.Miter] = "45-degree angled joint for clean corners (picture frames, trim).",
        [JoineryType.Dado] = "Groove cut across the grain to receive another piece (shelving).",
        [JoineryType.Rabbet] = "L-shaped cut on the edge, used for back panels and box construction.",
        [JoineryType.Groove] = "Channel cut along the grain for panel insertion.",
        [JoineryType.MortiseTenon] = "Strong traditional joint with a projecting tenon fitting into a cavity.",
        [JoineryType.ThroughMortise] = "Mortise and tenon where the tenon passes fully through.",
        [JoineryType.LooseTenon] = "Separate tenon piece inserted into mortises on both parts (Festool Domino).",
        [JoineryType.ThroughDovetail] = "Interlocking fan-shaped pins and tails, visible from both sides.",
        [JoineryType.HalfBlindDovetail] = "Dovetails hidden from the front face (drawer fronts).",
        [JoineryType.SlidingDovetail] = "Dovetail-shaped dado for strong shelf joints.",
        [JoineryType.BoxJoint] = "Interlocking rectangular fingers, great for boxes and drawers.",
        [JoineryType.Biscuit] = "Compressed wood oval inserted into matching slots for alignment.",
        [JoineryType.PocketHole] = "Angled screw joint for quick, strong face-frame assembly.",
        [JoineryType.Dowel] = "Round wooden pins inserted into aligned holes for reinforcement.",
        [JoineryType.TongueGroove] = "Interlocking edge joint for panels, flooring, and wide glue-ups.",
    };

    public static string GetTypeDisplayName(JoineryType type) => type switch
    {
        JoineryType.MortiseTenon => "Mortise & Tenon",
        JoineryType.ThroughMortise => "Through Mortise",
        JoineryType.LooseTenon => "Loose Tenon",
        JoineryType.ThroughDovetail => "Through Dovetail",
        JoineryType.HalfBlindDovetail => "Half-Blind Dovetail",
        JoineryType.SlidingDovetail => "Sliding Dovetail",
        JoineryType.BoxJoint => "Box Joint",
        JoineryType.PocketHole => "Pocket Hole",
        JoineryType.TongueGroove => "Tongue & Groove",
        _ => type.ToString()
    };

    partial void OnSelectedTypeChanged(JoineryType value)
    {
        Description = Descriptions.GetValueOrDefault(value, "Select a joinery type.");
    }

    [RelayCommand]
    private void Confirm()
    {
        DialogResult = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseRequested?.Invoke();
    }
}
