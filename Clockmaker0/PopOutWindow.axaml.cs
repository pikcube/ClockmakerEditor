using System;
using Avalonia.Controls;
using Clockmaker0.Data;

namespace Clockmaker0;

/// <summary>
/// Window for quickly loading any arbitrary control into its own window
/// </summary>
public partial class PopOutWindow : Window
{

    /// <summary>
    /// The currently loaded control
    /// </summary>
    public Control LoadedControl { get; set; }
    /// <summary>
    /// Public constructor for xaml preview, do not use
    /// </summary>
    public PopOutWindow() : this(new Control(), "Pop Out Window")
    {
    }

    /// <summary>
    /// Pop out a control into its own window
    /// </summary>
    /// <param name="control">The control to pop out</param>
    /// <param name="name">The title bar (default to the control's name)</param>
    public PopOutWindow(Control control, string? name = null)
    {
        name ??= control.GetType().Name;
        InitializeComponent();
        ScrollViewer.Content = control;
        LoadedControl = control;
        Title = name;
        Closed += PopOutWindow_Closed;
    }

    /// <summary>
    /// Pop out a control into its own window
    /// </summary>
    /// <param name="namedControl">The named control to pop out</param>
    public PopOutWindow(INamedControl<Control> namedControl) : this(namedControl.Control, namedControl.ControlName)
    {
        namedControl.NameChanged += NamedControl_NameChanged;
    }

    private void NamedControl_NameChanged(object? sender, string e)
    {
        Title = e;
    }

    private void PopOutWindow_Closed(object? sender, EventArgs e)
    {
        if (LoadedControl is INamedControl<Control> c)
        {
            c.NameChanged -= NamedControl_NameChanged;
        }

        if (LoadedControl is IDelete deleteable)
        {
            deleteable.Delete();
        }
    }
}