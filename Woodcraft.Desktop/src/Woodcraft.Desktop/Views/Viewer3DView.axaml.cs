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

    // Pan state
    private bool _isPanning;
    private Point _panStart;
    private double _panStartOffsetX;
    private double _panStartOffsetY;
    private double _panOffsetX;
    private double _panOffsetY;
    private double _zoomScale = 1.0;

    public Viewer3DView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerPressed += OnViewportPointerPressed;
        PointerMoved += OnViewportPointerMoved;
        PointerReleased += OnViewportPointerReleased;
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

        // Update joint lines in real-time while dragging
        if (vm != null)
        {
            UpdateJointLinesForModel(vm, _dragTarget);
        }

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

        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;

        var dialog = new AddJointDialog(dialogVm);
        await dialog.ShowDialog(mainWindow);

        if (dialogVm.DialogResult)
        {
            vm.AddJointCommand.Execute(dialogVm);
        }
    }

    private void UpdateJointLinesForModel(Viewer3DViewModel vm, Part3DModel movedModel)
    {
        if (movedModel.Part == null) return;
        var partId = movedModel.Part.Id;

        foreach (var jl in vm.JointLines)
        {
            // Find the matching joint to determine which end to move
            var project = vm.Project;
            if (project == null) continue;

            foreach (var joint in project.Joinery)
            {
                var modelA = vm.Models.FirstOrDefault(m => m.Part?.Id == joint.PartAId);
                var modelB = vm.Models.FirstOrDefault(m => m.Part?.Id == joint.PartBId);
                if (modelA == null || modelB == null) continue;

                if (joint.PartAId == partId || joint.PartBId == partId)
                {
                    jl.StartX = modelA.PositionX + modelA.SizeX / 2;
                    jl.StartY = modelA.PositionY + modelA.SizeY / 2;
                    jl.EndX = modelB.PositionX + modelB.SizeX / 2;
                    jl.EndY = modelB.PositionY + modelB.SizeY / 2;
                }
            }
        }

        RedrawJointLines();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var factor = delta > 0 ? 1.1 : 0.9;
        _zoomScale = Math.Clamp(_zoomScale * factor, 0.2, 5.0);
        ApplyViewportTransform();

        if (DataContext is Viewer3DViewModel vm)
            vm.CameraDistance = Math.Round(100 / _zoomScale);

        e.Handled = true;
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            _panStartOffsetX = _panOffsetX;
            _panStartOffsetY = _panOffsetY;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;

        var current = e.GetPosition(this);
        _panOffsetX = _panStartOffsetX + (current.X - _panStart.X);
        _panOffsetY = _panStartOffsetY + (current.Y - _panStart.Y);
        ApplyViewportTransform();
        e.Handled = true;
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void ApplyViewportTransform()
    {
        var viewport = this.FindControl<Grid>("ViewportGrid");
        if (viewport == null) return;

        viewport.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(_zoomScale, _zoomScale),
                new TranslateTransform(_panOffsetX, _panOffsetY),
            }
        };
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
