using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Woodcraft.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About Woodcraft",
            Width = 400,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(32),
                Spacing = 16,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Woodcraft",
                        FontSize = 28,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "CAD for Woodworking",
                        FontSize = 14,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Opacity = 0.7
                    },
                    new TextBlock
                    {
                        Text = "Version 1.0.0",
                        FontSize = 12,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Opacity = 0.5
                    },
                    new TextBlock
                    {
                        Text = "Design woodworking projects with parametric CAD, cut list optimization, and bill of materials generation.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        Opacity = 0.8,
                        MaxWidth = 320
                    },
                    new TextBlock
                    {
                        Text = "Powered by CadQuery and Avalonia UI",
                        FontSize = 11,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Opacity = 0.5,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0)
                    }
                }
            }
        };

        await dialog.ShowDialog(this);
    }
}
