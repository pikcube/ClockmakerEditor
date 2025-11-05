using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Printing;

public partial class PrintCharacterPreview : UserControl
{
    public PrintCharacterPreview() : this(MutableCharacter.Default, ScriptImageLoader.Default)
    {

    }

    public PrintCharacterPreview(MutableCharacter character, ScriptImageLoader loader)
    {
        InitializeComponent();
        NameTextBlock.Text = character.Name;
        AbilityTextBlock.Text = character.Ability;
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (loader == ScriptImageLoader.Default)
            {
                return;
            }
            CharacterImage.Source = await loader.GetImageAsync(character, 0);
        });
    }
}