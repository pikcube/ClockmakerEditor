using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.CharacterImport;

public partial class NightOrderPreview : UserControl
{
    public MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;
    public NightOrderPreview()
    {
        InitializeComponent();
    }

    public void Load(MutableCharacter character, ScriptImageLoader loader, string? nightReminderText)
    {
        SimpleCharacterPreview.Load(character, loader);
        LoadedCharacter = character;
        NightOrderText.Text = nightReminderText;
    }
}