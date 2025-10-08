using System;
using System.ComponentModel;
using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditMetaControls;

public partial class EditScriptMeta : UserControl
{
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;

    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPop;

    public EditScriptMeta()
    {
        InitializeComponent();
    }

    public void Load(MutableBotcScript loadedScript, ScriptImageLoader loader)
    {
        LoadedMeta = loadedScript.Meta;

        NameTextBox.Text = LoadedMeta.Name;
        AuthorTextBox.Text = LoadedMeta.Author;

        FirstNightOrderView.Load(LoadedMeta.FirstNight, loadedScript, loader, c => c.FirstNightReminder);
        FirstNightOrderView.OnPop += NightOrderView_OnPop;
        FirstNightOrderView.OnDelete += NightOrderView_OnDelete;
        OtherNightOrderView.Load(LoadedMeta.OtherNight, loadedScript, loader, c => c.OtherNightReminder);
        OtherNightOrderView.OnPop += NightOrderView_OnPop;
        OtherNightOrderView.OnDelete += NightOrderView_OnDelete;
        CustomBackgroundTab.Load(LoadedMeta, loader);
        CustomLogoTab.Load(LoadedMeta, loader);
        BootlegRulesTab.Load(LoadedMeta);
        AlmanacTab.Load(LoadedMeta);

        NameTextBox.TextChanged += NameTextBox_OnTextChanged;
        AuthorTextBox.TextChanged += AuthorTextBox_OnTextChanged;
        LoadedMeta.PropertyChanged += MetaInformation_PropertyChanged;
    }

    private void NightOrderView_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(sender, e);
    }

    private void NightOrderView_OnPop(object? sender, SimpleEventArgs<UserControl, string> e)
    {
        OnPop?.Invoke(sender, e);
    }

    private void MetaInformation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedMeta.Name):
                NameTextBox.Text = LoadedMeta.Name;
                break;
            case nameof(LoadedMeta.Author):
                AuthorTextBox.Text = LoadedMeta.Author;
                break;
        }
    }

    private void NameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedMeta.Name = NameTextBox.Text ?? "";
    }

    private void AuthorTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedMeta.Author = AuthorTextBox.Text;
    }
}