using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Woodcraft.Core.Models;
using Woodcraft.Desktop.ViewModels;

namespace Woodcraft.Desktop.Views;

public partial class Viewer3DView : UserControl
{
    private Part3DModel? _dragTarget;
    private Point _dragStart;
    private double _dragStartX;
    private double _dragStartY;
    private bool _isDragging;
    private const double DragThreshold = 4;
    private const double SnapDistance = 8;

    public Viewer3DView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is Viewer3DViewModel vm)
        {
            vm.JointLines.CollectionChanged += (_, _) => RedrawJointLines();
        }
    }

    private void RedrawJointLines()
    {
        var canvas = this.FindControl<Canvas>("JointLinesCanvas");
        if (canvas == null) return;

        canvas.Children.Clear();
        var vm = DataContext as Viewer3DViewModel;
        if (vm == null) return;

        foreach (var jl in vm.JointLines)
        {
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(jl.StartX, jl.StartY),
                EndPoint = new Point(jl.EndX, jl.EndY),
                StrokeThickness = 2,
                StrokeDashArray = [4, 4],
                Opacity = 0.7,
            };
            line.Stroke = Avalonia.Media.Brushes.Gold;
            canvas.Children.Add(line);

            // Joint label at midpoint
            var label = new Border
            {
                Padding = new Thickness(4, 2),
                CornerRadius = new CornerRadius(3),
                Background = Avalonia.Media.Brushes.Black,
                Opacity = 0.85,
                Child = new TextBlock
                {
                    Text = jl.Label,
                    FontSize = 9,
                    Foreground = Avalonia.Media.Brushes.Gold,
                }
            };
            Canvas.SetLeft(label, jl.MidX - 20);
            Canvas.SetTop(label, jl.MidY - 10);
            canvas.Children.Add(label);
        }
    }

    public void OnPartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Part3DModel model)
        {
            var vm = DataContext as Viewer3DViewModel;
            var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            // Select the part (Ctrl+click for secondary selection)
            vm?.SelectPartByModel(model, isCtrl);

            // Start potential drag
            _dragTarget = model;
            _dragStart = e.GetPosition(this);
            _dragStartX = model.PositionX;
            _dragStartY = model.PositionY;
            _isDragging = false;

            e.Pointer.Capture((IInputElement)sender);
            e.Handled = true;
        }
    }

    public void OnPartPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragTarget == null) return;

        var current = e.GetPosition(this);
        var deltaX = current.X - _dragStart.X;
        var deltaY = current.Y - _dragStart.Y;

        // Only start dragging after passing threshold
        if (!_isDragging)
        {
            if (Math.Abs(deltaX) < DragThreshold && Math.Abs(deltaY) < DragThreshold)
                return;
            _isDragging = true;
        }

        var newX = _dragStartX + deltaX;
        var newY = _dragStartY + deltaY;

        // Snap to other parts' edges
        var vm = DataContext as Viewer3DViewModel;
        if (vm != null)
        {
            (newX, newY) = SnapToEdges(vm, _dragTarget, newX, newY);
        }

        _dragTarget.PositionX = newX;
        _dragTarget.PositionY = newY;

        e.Handled = true;
    }

    public void OnPartPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragTarget != null)
        {
            if (_isDragging)
            {
                var vm = DataContext as Viewer3DViewModel;
                vm?.UpdatePartPosition(_dragTarget);
            }

            e.Pointer.Capture(null);
            _dragTarget = null;
            _isDragging = false;
            e.Handled = true;
        }
    }

    private async void OnAddJointClick(object? sender, RoutedEventArgs e)
    {
        var vm = DataContext as Viewer3DViewModel;
        if (vm?.SelectedPart == null || vm.SecondarySelectedPart == null) return;

        var dialogVm = new AddJointDialogViewModel
        {
            PartAName = vm.SelectedPart.Id,
            PartBName = vm.SecondarySelectedPart.Id,
        };

        var dialog = BuildAddJointDialog(dialogVm);

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;

        await dialog.ShowDialog(mainWindow);

        if (dialogVm.DialogResult)
        {
            vm.AddJointCommand.Execute(dialogVm);
        }
    }

    private static Window BuildAddJointDialog(AddJointDialogViewModel vm)
    {
        var window = new Window
        {
            Title = "Join Parts",
            Width = 420,
            Height = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            DataContext = vm,
        };

        // Build UI programmatically (same pattern as AddHardwareDialog)
        var panel = new DockPanel { Margin = new Thickness(20) };

        // Header
        var header = new TextBlock
        {
            Text = "Join Parts",
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 16),
        };
        DockPanel.SetDock(header, Dock.Top);
        panel.Children.Add(header);

        // Part names display
        var partInfo = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 16) };
        var partALabel = new TextBlock { FontSize = 12 };
        partALabel.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("PartAName") { StringFormat = "Part A: {0}" });
        var partBLabel = new TextBlock { FontSize = 12 };
        partBLabel.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("PartBName") { StringFormat = "Part B: {0}" });
        partInfo.Children.Add(partALabel);
        partInfo.Children.Add(partBLabel);
        DockPanel.SetDock(partInfo, Dock.Top);
        panel.Children.Add(partInfo);

        // Joinery type selector
        var typeLabel = new TextBlock
        {
            Text = "Joint Type",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        };
        DockPanel.SetDock(typeLabel, Dock.Top);
        panel.Children.Add(typeLabel);

        var typeList = new ListBox
        {
            MaxHeight = 200,
            Margin = new Thickness(0, 0, 0, 8),
        };
        typeList.Bind(ListBox.ItemsSourceProperty, new Avalonia.Data.Binding("JoineryTypes"));
        typeList.Bind(ListBox.SelectedItemProperty, new Avalonia.Data.Binding("SelectedType"));
        DockPanel.SetDock(typeList, Dock.Top);
        panel.Children.Add(typeList);

        // Description
        var descLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 16),
        };
        descLabel.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Description"));
        DockPanel.SetDock(descLabel, Dock.Top);
        panel.Children.Add(descLabel);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12,
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var cancelBtn = new Button { Content = "Cancel", Padding = new Thickness(20, 10) };
        cancelBtn.Click += (_, _) => { vm.DialogResult = false; window.Close(); };
        buttonPanel.Children.Add(cancelBtn);

        var confirmBtn = new Button
        {
            Content = "Create Joint",
            Padding = new Thickness(20, 10),
            Classes = { "primary" },
        };
        confirmBtn.Click += (_, _) => { vm.DialogResult = true; window.Close(); };
        buttonPanel.Children.Add(confirmBtn);

        panel.Children.Add(buttonPanel);
        window.Content = panel;

        vm.CloseRequested = () => window.Close();

        return window;
    }

    private (double x, double y) SnapToEdges(Viewer3DViewModel vm, Part3DModel dragging, double x, double y)
    {
        double snappedX = x;
        double snappedY = y;

        double dragLeft = x;
        double dragRight = x + dragging.SizeX;
        double dragTop = y;
        double dragBottom = y + dragging.SizeY;

        foreach (var other in vm.Models)
        {
            if (other == dragging) continue;

            double otherLeft = other.PositionX;
            double otherRight = other.PositionX + other.SizeX;
            double otherTop = other.PositionY;
            double otherBottom = other.PositionY + other.SizeY;

            // Snap left edge to right edge of other
            if (Math.Abs(dragLeft - otherRight) < SnapDistance)
                snappedX = otherRight;
            // Snap right edge to left edge of other
            else if (Math.Abs(dragRight - otherLeft) < SnapDistance)
                snappedX = otherLeft - dragging.SizeX;
            // Snap left edges aligned
            else if (Math.Abs(dragLeft - otherLeft) < SnapDistance)
                snappedX = otherLeft;
            // Snap right edges aligned
            else if (Math.Abs(dragRight - otherRight) < SnapDistance)
                snappedX = otherRight - dragging.SizeX;

            // Snap top edge to bottom edge of other
            if (Math.Abs(dragTop - otherBottom) < SnapDistance)
                snappedY = otherBottom;
            // Snap bottom edge to top edge of other
            else if (Math.Abs(dragBottom - otherTop) < SnapDistance)
                snappedY = otherTop - dragging.SizeY;
            // Snap top edges aligned
            else if (Math.Abs(dragTop - otherTop) < SnapDistance)
                snappedY = otherTop;
            // Snap bottom edges aligned
            else if (Math.Abs(dragBottom - otherBottom) < SnapDistance)
                snappedY = otherBottom - dragging.SizeY;
        }

        return (snappedX, snappedY);
    }
}
