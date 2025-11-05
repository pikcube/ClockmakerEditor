using System;
using Avalonia.Controls;

namespace Clockmaker0.Data;

/// <summary>
/// A control with a default name to display when contained within its own window
/// </summary>
public interface INamedControl<out T> where T : Control
{
    /// <summary>
    /// The name of the control when displayed in its own window
    /// </summary>
    public string ControlName { get; }

    /// <summary>
    /// The control
    /// </summary>
    public T Control { get; }

    /// <summary>
    /// Raised when the name of the named control changes
    /// </summary>
    public event EventHandler<string>? NameChanged;
}