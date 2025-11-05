using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.CharacterImport;

/// <summary>
/// UI Control Representing a Single Night Order Item
/// </summary>
public partial class NightOrderPreview : UserControl
{
    /// <summary>
    /// The character currently loaded in the night order
    /// </summary>
    public MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;

    /// <inheritdoc />
    public NightOrderPreview()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load in the current character and initialize all fields with data
    /// </summary>
    /// <param name="character">The character to be assigned to LoadedCharacter</param>
    /// <param name="loader">The script image loader</param>
    /// <param name="nightReminderText">The reminder text</param>
    public void Load(MutableCharacter character, ScriptImageLoader loader, string? nightReminderText)
    {
        SimpleCharacterPreview.Load(character, loader);
        LoadedCharacter = character;
        NightOrderText.Text = nightReminderText;
    }
}