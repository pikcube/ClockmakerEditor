using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class Advanced : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;

    public Advanced()
    {
        InitializeComponent();

    }

    public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader)
    {
        LoadedCharacter = loadedCharacter;
        LoadedScript = loadedScript;
        Loader = loader;
        CharacterJson.Text = JsonConvert.SerializeObject(LoadedCharacter.ToImmutable(loadedScript.Jinxes), Formatting.Indented);
        CharacterJson.IsReadOnly = true;
        LoadedCharacter.PropertyChanged += LoadedCharacter_PropertyChanged;
        EditButton.Click += EditButton_Click;
    }

    private void EditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (EditButton.Content?.ToString() == "Edit")
        {
            CharacterJson.IsReadOnly = false;
            EditButton.Content = "Save";
        }
        else
        {
            CharacterJson.IsReadOnly = true;
            EditButton.Content = "Edit";
            try
            {
                Character? c = JsonConvert.DeserializeObject<Character>(CharacterJson.Text ?? "");
                if (c is null)
                {
                    return;
                }

                c.AsMutable().CopyTo(LoadedCharacter);
            }
            catch (Exception exception)
            {
                TaskManager.ScheduleTask(async () =>
                {
                    if (TopLevel.GetTopLevel(this) is Window w)
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Error", exception.Message, ButtonEnum.Ok,
                                Icon.Error,
                                WindowStartupLocation.CenterOwner)
                            .ShowWindowDialogAsync(w);
                    }
                    else
                    {
                        //wut?
                        await MessageBoxManager.GetMessageBoxStandard("Error", exception.Message, ButtonEnum.Ok,
                                Icon.Error,
                                WindowStartupLocation.CenterOwner)
                            .ShowAsPopupAsync(this);
                    }

                });
            }
            finally
            {
                Loader.ReloadAll();
            }
        }
    }

    private void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Reload();
    }

    private void Reload()
    {
        CharacterJson.Text = JsonConvert.SerializeObject(LoadedCharacter.ToImmutable(LoadedScript.Jinxes), Formatting.Indented);
    }

    public void Delete()
    {
        LoadedCharacter.PropertyChanged -= LoadedCharacter_PropertyChanged;
    }

    public void Unlock()
    {
        EditButton.IsEnabled = true;
    }

    public void Lock()
    {
        EditButton.IsEnabled = false;
    }
}