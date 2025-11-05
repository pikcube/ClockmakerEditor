using Clockmaker0.Controls.CharacterImport;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Data;

/// <summary>
/// Class that encapsulates all the data for dragging data around
/// </summary>
/// <param name="nop">Night Order Preview We Are Dragging</param>
/// <param name="list">List that the Night Order Preview Belongs To</param>
public class NightOrderDrag(NightOrderPreview nop, TrackedList<MutableCharacter> list)
{
    /// <summary>
    /// Preview we are dragging
    /// </summary>
    public NightOrderPreview Preview { get; } = nop;
    /// <summary>
    /// Character we are dragging
    /// </summary>
    public MutableCharacter LoadedCharacter => Preview.LoadedCharacter;
    /// <summary>
    /// List the drag started in
    /// </summary>
    public TrackedList<MutableCharacter> List { get; } = list;
}