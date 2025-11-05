using System;
using Clockmaker0.Controls.EditCharacterControls;

namespace Clockmaker0.Data;

/// <summary>
/// Interface that means a control may attempt to pop out a control into its own window. Controls are responsible for propogating this up to the Main Window which will pop out the control
/// </summary>
public interface IOnPop
{
    /// <summary>
    /// Raised when a Control wishes to be popped out into a new Window. Controls are responsible for propogating this up to the Main Window which will pop out the control
    /// </summary>
    public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPop;
}