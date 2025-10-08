using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Controls.CharacterImport;

public partial class OfficialCharacterPreview : UserControl
{
    public Character LoadedCharacter { get; private set; } = Character.Default;


    public OfficialCharacterPreview()
    {
        InitializeComponent();
    }

    public bool IsImported => ImportCheckBox.IsChecked is true;

    public OfficialCharacterPreview Load(Character character, ScriptImageLoader imageLoader)
    {
        SimpleCharacterPreview.Load(character, imageLoader);
        LoadedCharacter = character;
        return this;
    }
}