using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

/// <summary>
/// Control for editting a jinx
/// </summary>
public partial class EditJinx : UserControl, IDelete
{
    private MutableJinx LoadedJinx { get; set; } = new("", Character.Default.Id, Character.Default.Id);
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    private List<ICharacter> Source { get; set; } = [];

    /// <summary>
    /// Raised when this control is to be deleted, so the caller can detach it from the visual tree.
    /// </summary>
    public event EventHandler<SimpleEventArgs<EditJinx>>? OnDelete;

    /// <inheritdoc />
    public EditJinx()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the jinx data into the control for editting
    /// </summary>
    /// <param name="jinx">The jinx to load</param>
    /// <param name="loadedScript">The script the jinx belongs to</param>
    public void Load(MutableJinx jinx, MutableBotcScript loadedScript)
    {
        LoadedJinx = jinx;
        LoadedScript = loadedScript;

        string[] sources = ["On Script", "Official Repo", "Homebrew"];
        SourceComboBox.SelectionChanged += SourceComboBox_SelectionChanged;
        ChildComboBox.SelectionChanged += TargetComboBox_SelectionChanged;
        ChildTextBox.TextChanged += ChildTextBox_TextChanged;
        SourceComboBox.ItemsSource = sources;

        InitializeSource(jinx);

        JinxTextBox.Text = jinx.Rule;


        jinx.OnDelete += (_, _) =>
        {
            OnDelete?.Invoke(this, new SimpleEventArgs<EditJinx>(this));
        };
        jinx.PropertyChanged += Jinx_PropertyChanged;
        JinxTextBox.TextChanged += JinxTextBoxTextChanged;
    }

    private void ChildTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedJinx.Child = ChildTextBox.Text ?? LoadedJinx.Child;
    }

    private void SourceComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ChildComboBox.SelectionChanged -= TargetComboBox_SelectionChanged;

        Source = SourceComboBox.SelectedIndex switch
        {
            0 => [.. GetSource(CharacterSource.Script)],
            1 => [.. GetSource(CharacterSource.Official)],
            _ => [],
        };

        if (Source.Count == 0)
        {
            ChildComboBox.IsEnabled = false;
            ChildComboBox.IsVisible = false;
            ChildTextBox.IsEnabled = true;
            ChildTextBox.IsVisible = true;
            ChildComboBox.ItemsSource = new[] { LoadedJinx.Child };
            ChildComboBox.SelectedItem = LoadedJinx.Child;

        }
        else
        {
            ChildComboBox.IsEnabled = true;
            ChildComboBox.IsVisible = true;
            ChildTextBox.IsEnabled = false;
            ChildTextBox.IsVisible = false;
            ChildComboBox.ItemsSource = Source.Select(c => c.Name);
            ChildComboBox.SelectedItem = Source.FirstOrDefault(c => c.Id == LoadedJinx.Child)?.Name;
            ChildComboBox.SelectionChanged += TargetComboBox_SelectionChanged;
        }
    }

    private void InitializeSource(MutableJinx jinx)
    {
        CharacterSource location = FindCharacter(jinx.Child);

        SourceComboBox.SelectedIndex = LowestFlag(location) switch
        {
            CharacterSource.Script => 0,
            CharacterSource.Official => 1,
            _ => 2
        };
        ChildTextBox.Text = jinx.Child;
    }

    private static CharacterSource LowestFlag(CharacterSource location)
    {
        if (location == 0)
        {
            return 0;
        }
        return (CharacterSource)(1 << BitOperations.TrailingZeroCount((int)location));
    }

    private void TargetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadedJinx.Child = GetChildId(ChildComboBox.SelectedIndex);
    }

    private string GetChildId(int selectedIndex) => selectedIndex == -1 ? LoadedJinx.Child : Source[selectedIndex].Id;

    private IEnumerable<ICharacter> GetSource(CharacterSource source)
    {
        return LowestFlag(source) switch
        {
            CharacterSource.Script => Track(LoadedScript.Characters),
            CharacterSource.Official => ScriptParse.GetOfficialCharacters.Values.OrderBy(c => c.Name),
            _ => []
        };
    }

    private CharacterSource FindCharacter(string jinxChild)
    {
        CharacterSource onScript = LoadedScript.Characters.Any(c => c.Id == jinxChild) ? CharacterSource.Script : CharacterSource.NotFound;
        CharacterSource official = ScriptParse.IsOfficial(jinxChild) ? CharacterSource.Official : CharacterSource.NotFound;
        return official | onScript;
    }

    private TrackedList<MutableCharacter> Track(TrackedList<MutableCharacter> loadedScriptCharacters)
    {
        loadedScriptCharacters.ItemAdded += Characters_ItemAdded;
        loadedScriptCharacters.ItemRemoved += Characters_ItemRemoved;
        loadedScriptCharacters.OrderChanged += Characters_OrderChanged;
        return loadedScriptCharacters;
    }

    private void Characters_OrderChanged(object? sender, ValueChangedArgs<TrackedList<MutableCharacter>> e)
    {
        ChildComboBox.ItemsSource = e.NewValue.Select(c => c.Name);
        ChildComboBox.SelectedItem = Source.FirstOrDefault(c => c.Id == LoadedJinx.Child)?.Name;
    }

    private void Characters_ItemRemoved(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        Source.Remove(e.NewValue);
        ChildComboBox.ItemsSource = Source.Select(c => c.Name);
        if (LoadedJinx.Child != e.NewValue.Id)
        {
            ChildComboBox.SelectedItem = Source.FirstOrDefault(c => c.Id == LoadedJinx.Child)?.Name;
        }
        else
        {
            ChildComboBox.SelectedIndex = -1;
        }

    }

    private void Characters_ItemAdded(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        Source.Add(e.NewValue);
        ChildComboBox.ItemsSource = Source.Select(c => c.Name);
        ChildComboBox.SelectedItem = Source.FirstOrDefault(c => c.Id == LoadedJinx.Child)?.Name;
    }

    private void JinxTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedJinx.Rule = JinxTextBox.Text ?? LoadedJinx.Rule;
    }

    private void Jinx_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedJinx.Child):
                SetTarget(LoadedJinx.Child);
                break;

            case nameof(LoadedJinx.Rule):
                JinxTextBox.Text = LoadedJinx.Rule;
                break;
        }
    }

    private void SetTarget(string loadedJinxChild)
    {
        ICharacter? character = Source.FirstOrDefault(c => c.Id == loadedJinxChild);
        if (character is not null)
        {
            ChildComboBox.SelectedIndex = Source.IndexOf(character);
            ChildTextBox.Text = loadedJinxChild;
            return;
        }

        InitializeSource(LoadedJinx);
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (!App.IsKeyDown(Key.RightShift, Key.LeftShift))
            {
                IMsBox<ButtonResult> msg = MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    "Are you sure you want to delete this jinx?", ButtonEnum.YesNo);

                ButtonResult result = TopLevel.GetTopLevel(this) is Window window
                    ? await msg.ShowWindowDialogAsync(window)
                    : await msg.ShowAsPopupAsync(this);

                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }

            LoadedJinx.Delete();
        });
    }

    /// <inheritdoc />
    public void Delete()
    {
        LoadedJinx.Delete();
    }

    /// <summary>
    /// Source of the jinx
    /// </summary>
    [Flags]
    public enum CharacterSource
    {
        /// <summary>
        /// Can't find the source
        /// </summary>
        NotFound = 0,
        /// <summary>
        /// On Script Character
        /// </summary>
        Script = 1,
        /// <summary>
        /// In the Official Repo
        /// </summary>
        Official = 2,
    }
}