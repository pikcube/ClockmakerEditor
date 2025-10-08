using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Controls.EditMetaControls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.CharacterPreview;

public partial class ScriptTitle : UserControl
{
    private EditScriptMeta? _meta;
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;
    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPop;
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
    public event EventHandler<SimpleEventArgs<UserControl>>? OnLoadEdit;
    public event EventHandler<SimpleEventArgs<UserControl>>? OnAddCharacter;

    public ScriptTitle()
    {
        InitializeComponent();
        IsEnabled = false;
    }

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
        if (_meta is null)
        {
            _meta = new EditScriptMeta();
            _meta.Load(LoadedScript, Loader);
            _meta.OnDelete += Meta_OnDelete;
            _meta.OnPop += Meta_OnPop;
        }

        OnLoadEdit?.Invoke(this, new SimpleEventArgs<UserControl>(_meta));
    }

    private void Meta_OnPop(object? sender, SimpleEventArgs<UserControl, string> e)
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