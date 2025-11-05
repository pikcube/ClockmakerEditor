using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Controls.CharacterImport;

/// <summary>
/// Control for marking a character for import
/// </summary>
public partial class OfficialCharacterPreview : UserControl
{
    /// <summary>
    /// The currently loaded character
    /// </summary>
    public Character LoadedCharacter { get; private set; } = Character.Default;


    /// <inheritdoc />
    public OfficialCharacterPreview()
    {
        InitializeComponent();
    }

    /// <summary>
    /// True when the ImportCheckBox is checked
    /// </summary>
    public bool IsImported => ImportCheckBox.IsChecked is true;

    /// <summary>
    /// Load the character for import
    /// </summary>
    /// <param name="character"></param>
    /// <param name="imageLoader"></param>
    /// <returns></returns>
    public OfficialCharacterPreview Load(Character character, ScriptImageLoader imageLoader)
    {
        SimpleCharacterPreview.Load(character, imageLoader);
        LoadedCharacter = character;
        return this;
    }
}