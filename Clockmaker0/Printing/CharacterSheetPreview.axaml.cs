using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Skia.Helpers;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using SkiaSharp;

namespace Clockmaker0.Printing;

public partial class CharacterSheetPreview : Window
{
    public CharacterSheetPreview() : this(BotcScript.Default.ToMutable(), ScriptImageLoader.Default)
    {

    }

    public CharacterSheetPreview(MutableBotcScript script, ScriptImageLoader loader)
    {
        InitializeComponent();
        CharacterStackPanel.Children.Add(new ScriptTitlePreview(script, loader));
        foreach (MutableCharacter character in script.Characters.Where(c => c.Team is not TeamEnum.Special && c.Team is not TeamEnum.Fabled && c.Team is not TeamEnum.Traveller))
        {
            CharacterStackPanel.Children.Add(new PrintCharacterPreview(character, loader));
        }


        Loaded += CharacterSheetPreview_Loaded;
    }


    private void CharacterSheetPreview_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            using IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                FileTypeChoices =
                [
                    new FilePickerFileType("PDf")
                    {
                        Patterns = ["*.pdf"]
                    }
                ],
                SuggestedFileName = "test.pdf"
            });
            if (file is null)
            {
                return;
            }

            await using Stream write = await file.OpenWriteAsync();
            using SKDocument skdoc = SKDocument.CreatePdf(write);
            using SKCanvas canvas = skdoc.BeginPage(850, 1100);
            await DrawingContextHelper.RenderAsync(canvas, CharacterStackPanel);
            canvas.Save();
            skdoc.EndPage();

        });
    }
}