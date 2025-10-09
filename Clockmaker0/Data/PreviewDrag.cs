using Clockmaker0.Controls.CharacterPreview;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Data;

public class PreviewDrag(PreviewScriptCharacter psc, MutableCharacter loadedCharacter, Character toImmutable, MutableBotcScript loadedScript, ScriptImageLoader imageLoader)
{
    public PreviewScriptCharacter Preview { get; } = psc;
    public MutableCharacter LoadedCharacter { get; } = loadedCharacter;
    public Character ImmutableCharacter { get; } = toImmutable;
    public MutableBotcScript LoadedScript { get; } = loadedScript;
    public ScriptImageLoader Loader { get; } = imageLoader;
}