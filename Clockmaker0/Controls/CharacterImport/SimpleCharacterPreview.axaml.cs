using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Controls.CharacterImport;

/// <summary>
/// A basic control to view a character with its icon
/// </summary>
public partial class SimpleCharacterPreview : UserControl
{
    private ScriptImageLoader ImageLoader { get; set; } = ScriptImageLoader.Default;

    private ICharacter LoadedCharacter { get; set; } = Character.Default;

    /// <inheritdoc />
    public SimpleCharacterPreview()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the character's name and image
    /// </summary>
    /// <param name="character">Character object</param>
    /// <param name="imageLoader">Character's image loader</param>
    public void Load(ICharacter character, ScriptImageLoader imageLoader)
    {
        LoadedCharacter = character;
        NameTextBox.Text = character.Name;
        ImageLoader = imageLoader;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0);
            ImageLoader.ReloadImage += ImageLoader_ReloadImage;
        });
    }

    private void ImageLoader_ReloadImage(object? sender, KeyArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (e.Key is null)
            {
                CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0);
                return;
            }
            foreach (string s in LoadedCharacter.Image)
            {
                if (s == e.Key)
                {
                    CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0);
                }
            }
        });
    }
}