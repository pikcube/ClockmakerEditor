using Clockmaker0.Controls.CharacterImport;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Data;

public class NightOrderDrag(NightOrderPreview nop, MutableCharacter character, TrackedList<MutableCharacter> list)
{
    public NightOrderPreview Preview { get; } = nop;
    public MutableCharacter LoadedCharacter { get; } = character;
    public TrackedList<MutableCharacter> List { get; } = list;
}