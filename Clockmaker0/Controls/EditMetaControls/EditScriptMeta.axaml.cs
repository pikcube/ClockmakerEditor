using Avalonia.Controls;
using Clockmaker0.Controls.EditCharacterControls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;
using System;
using System.ComponentModel;

namespace Clockmaker0.Controls.EditMetaControls;

/// <summary>
/// Control for editing the meta property of a script
/// </summary>
public partial class EditScriptMeta : UserControl, IOnDelete, IOnPop
{
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPop;

    /// <inheritdoc />
    public EditScriptMeta()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the current meta into the control
    /// </summary>
    /// <param name="loadedScript">The script to load in</param>
    /// <param name="loader">The script's image loader</param>
    public void Load(MutableBotcScript loadedScript, ScriptImageLoader loader)
    {
        LoadedMeta = loadedScript.Meta;

        NameTextBox.Text = LoadedMeta.Name;
        AuthorTextBox.Text = LoadedMeta.Author;

        FirstNightOrderView.Load(LoadedMeta.FirstNight, loadedScript, loader, c => c.FirstNightReminder);
        OtherNightOrderView.Load(LoadedMeta.OtherNight, loadedScript, loader, c => c.OtherNightReminder);
        CustomBackgroundTab.Load(LoadedMeta, loader);
        CustomLogoTab.Load(LoadedMeta, loader);
        BootlegRulesTab.Load(LoadedMeta);
        AlmanacTab.Load(LoadedMeta);

        FirstNightOrderView.OnPop += NightOrderView_OnPop;
        FirstNightOrderView.OnDelete += NightOrderView_OnDelete;
        OtherNightOrderView.OnPop += NightOrderView_OnPop;
        OtherNightOrderView.OnDelete += NightOrderView_OnDelete;
        NameTextBox.TextChanged += NameTextBox_OnTextChanged;
        AuthorTextBox.TextChanged += AuthorTextBox_OnTextChanged;
        LoadedMeta.PropertyChanged += MetaInformation_PropertyChanged;
    }

    private void NightOrderView_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(sender, e);
    }

    private void NightOrderView_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
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