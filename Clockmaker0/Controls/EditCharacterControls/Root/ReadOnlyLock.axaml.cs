using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;

namespace Clockmaker0.Controls.EditCharacterControls.Root;

/// <inheritdoc />
public partial class ReadOnlyLock : UserControl
{
    private ScriptImageLoader ImageLoader { get; set; } = ScriptImageLoader.Default;

    /// <summary>
    /// Raised when the fork button is pressed
    /// </summary>
    public event EventHandler<EventArgs>? OnFork;

    /// <inheritdoc />
    public ReadOnlyLock()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        OnFork?.Invoke(this, e);
    }

    /// <summary>
    /// Makes the control disabled and invisible
    /// </summary>
    public void Delete()
    {
        IsEnabled = false;
        IsVisible = false;
    }
}