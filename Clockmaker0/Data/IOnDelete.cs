using Pikcube.ReadWriteScript.Core.Mutable;
using System;

namespace Clockmaker0.Data;

/// <summary>
/// Interface that means a control may attempt delete a character from the script. Controls are responsible for propogating this up to the Main Window which will delete the character and call MutableCharacter.Delete
/// </summary>

public interface IOnDelete
{
    /// <summary>
    /// Raised when a character is to be deleted from the script. Controls are responsible for propogating this up to the Main Window which will delete the character and call MutableCharacter.Delete
    /// </summary>
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
}