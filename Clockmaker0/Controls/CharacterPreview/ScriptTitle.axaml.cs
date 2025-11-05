using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Controls.EditCharacterControls;
using Clockmaker0.Controls.EditMetaControls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using System;
using System.ComponentModel;

namespace Clockmaker0.Controls.CharacterPreview;

/// <summary>
/// Script item for the script's title
/// </summary>
public partial class ScriptTitle : UserControl, IOnPop, IOnDelete
{
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPop;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
    /// <summary>
    /// Raised when the Expand Button is clicked
    /// </summary>
    public event EventHandler<SimpleEventArgs<UserControl>>? OnLoadEdit;
    /// <summary>
    /// Raised when a new character is added to the script
    /// </summary>
    public event EventHandler<SimpleEventArgs<UserControl>>? OnAddCharacter;

    /// <inheritdoc />
    public ScriptTitle()
    {
        InitializeComponent();
        IsEnabled = false;
    }

    /// <summary>
    /// Load the current script info
    /// </summary>
    /// <param name="script">The script</param>
    /// <param name="loader">The image loader</param>
    public void Load(MutableBotcScript script, ScriptImageLoader loader)
    {
        LoadedScript = script;
        LoadedMeta = script.Meta;
        Loader = loader;

        TitleTextBox.Text = LoadedMeta.Name;
        AuthorTextBox.Text = LoadedMeta.Author;

        TitleTextBox.TextChanged += TitleTextBox_TextChanged;
        AuthorTextBox.TextChanged += AuthorTextBox_TextChanged;

        LoadedMeta.PropertyChanged += MetaInformation_PropertyChanged;

        IsEnabled = true;
    }

    private void MetaInformation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedMeta.Name):
                TitleTextBox.Text = LoadedMeta.Name;
                break;
            case nameof(LoadedMeta.Author):
                AuthorTextBox.Text = LoadedMeta.Author;
                break;
        }
    }

    private void AuthorTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedMeta.Author = AuthorTextBox.Text;
    }

    private void TitleTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedMeta.Name = TitleTextBox.Text ?? LoadedMeta.Name;
    }

    private void ExpandButton_OnClick(object? sender, RoutedEventArgs e)
    {
        EditScriptMeta meta = new EditScriptMeta();
        meta.Load(LoadedScript, Loader);
        meta.OnDelete += Meta_OnDelete;
        meta.OnPop += Meta_OnPop;

        OnLoadEdit?.Invoke(this, new SimpleEventArgs<UserControl>(meta));
    }

    private void Meta_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
    {
        OnPop?.Invoke(sender, e);
    }

    private void Meta_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(sender, e);
    }

    private void NewCharacterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        OnAddCharacter?.Invoke(sender, new SimpleEventArgs<UserControl>(this));
    }
}