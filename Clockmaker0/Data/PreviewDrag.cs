using Clockmaker0.Controls.CharacterPreview;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Data;

/// <summary>
/// Object that encapsulates a drag of a character preview from the script panel
/// </summary>
/// <param name="psc">The preview being dragged</param>
public class PreviewDrag(PreviewScriptCharacter psc)
{
    /// <summary>
    /// The script preview object
    /// </summary>
    public PreviewScriptCharacter Preview { get; } = psc;
    /// <summary>
    /// The character being dragged
    /// </summary>
    public MutableCharacter LoadedCharacter => Preview.LoadedCharacter;
    /// <summary>
    /// The script the character is from
    /// </summary>
    public MutableBotcScript LoadedScript => Preview.LoadedScript;
    /// <summary>
    /// The script image loader
    /// </summary>
    public ScriptImageLoader Loader => Preview.ImageLoader;
}