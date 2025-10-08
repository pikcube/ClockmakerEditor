using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Root;

public partial class BasicCharacterInfo : UserControl
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;

    private BotcScript LoadedScript { get; set; } = BotcScript.Default;
    private static SortEnumBox[] SortIndex { get; } = [.. Enum.GetValues<SortType>()[1..].Select(st => new SortEnumBox(st))];

    public BasicCharacterInfo()
    {
        InitializeComponent();
        CharacterTeamComboBox.ItemsSource = Enum.GetValues<TeamEnum>()[1..];
        SortComboBox.ItemsSource = SortIndex;
    }
    private void CharacterAbilityTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = CharacterAbilityTextBox.Text ?? "";

        LoadedCharacter.Ability = text;
    }

    private void CharacterNameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedCharacter.Name = CharacterNameTextBox.Text ?? "";
    }

    private void CharacterFlavorTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedCharacter.Flavor = CharacterFlavorTextBox.Text ?? "";
    }

    private void CharacterTeamComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadedCharacter.Team = (TeamEnum?)CharacterTeamComboBox.SelectedItem;
    }
    private void EditionTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedCharacter.Edition = EditionTextBox.Text ?? "";
    }

    private void SortComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SortEnumBox selectedItem = (SortEnumBox)(SortComboBox.SelectedItem ?? throw new InvalidOperationException());

        if (selectedItem == SortType.None || LoadedCharacter.SortInfo == selectedItem)
        {
            return;
        }

        string sortKey = LoadedCharacter.GetSortKey() ?? "";
        string oldKey = LoadedCharacter.Ability[..sortKey.Length].Trim();
        string newKey = "";
        if (selectedItem.Value is not SortType.Other)
        {
            newKey = Extensions.SortList[(int)selectedItem.Value - 1].Trim();
        }

        if (oldKey == "")
        {
            LoadedCharacter.Ability = newKey + LoadedCharacter.Ability;
        }
        else
        {
            LoadedCharacter.Ability = LoadedCharacter.Ability.Replace(oldKey, newKey);
        }
    }

    private void SetupTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!SetupTextBox.IsEnabled)
        {
            return;
        }
        string? text = SetupTextBox.Text;
        int caretIndex = SetupTextBox.CaretIndex;

        if (string.IsNullOrWhiteSpace(text))
        {
            LoadedCharacter.Ability = LoadedCharacter.Ability.Split('[', 2).First().TrimEnd();
            return;
        }


        if (BadTextFilter(ref text, ref caretIndex))
        {
            SetupTextBox.CaretIndex = caretIndex;
            SetupTextBox.Text = text;
        }

        LoadedCharacter.Ability = $"{LoadedCharacter.Ability.Split('[', 2).First().TrimEnd()} [{text}]";
    }

    private static bool BadTextFilter(ref string text, ref int caretIndex, int openNum = 0, int closeNum = 0)
    {
        int open = text.Count(c => c is '[');
        int closed = text.Count(c => c is ']');

        if (open <= openNum && closed <= closeNum)
        {
            return false;
        }

        int beforeCount = 0;
        List<char> before = [.. text[..caretIndex]];
        List<char> after = [.. text[caretIndex..]];
        List<char> final = new(text.Length);
        while (before.Count > 0)
        {
            char current = before[0];
            before.RemoveAt(0);
            switch (current)
            {
                case '[' when openNum > 0:
                    --openNum;
                    break;
                case '[':
                    continue;
                case ']' when closeNum > 0:
                    --closeNum;
                    break;
                case ']':
                    continue;
            }

            final.Add(current);
            ++beforeCount;
        }

        while (after.Count > 0)
        {
            char current = after[0];
            after.RemoveAt(0);
            switch (current)
            {
                case '[' when openNum > 0:
                    --openNum;
                    break;
                case '[':
                    continue;
                case ']' when closeNum > 0:
                    --closeNum;
                    break;
                case ']':
                    continue;
            }

            final.Add(current);
        }

        text = string.Join("", final);
        caretIndex = beforeCount;

        return true;
    }

    private void UpdateSetup()
    {
        int openCount = LoadedCharacter.Ability.Count(c => c == '[');
        int closeCount = LoadedCharacter.Ability.Count(c => c == ']');
        switch (openCount, closeCount)
        {
            case (0, 0):
                SetupTextBox.Text = "";
                LoadedCharacter.Setup = false;
                SetupTextBox.IsEnabled = true;
                return;
            case (1, 1):
                {
                    int start = LoadedCharacter.Ability.IndexOf('[') + 1;
                    int end = LoadedCharacter.Ability.IndexOf(']');
                    if (start > end)
                    {
                        goto default;
                    }
                    SetupTextBox.Text = LoadedCharacter.Ability[start..end];
                    LoadedCharacter.Setup = true;
                    SetupTextBox.IsEnabled = true;
                    return;
                }
            default:
                LoadedCharacter.Setup = false;
                SetupTextBox.IsEnabled = false;
                SetupTextBox.Text = "Invalid Brackets in Ability";
                return;
        }
    }

    private void SetEditionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadedCharacter.Edition = LoadedScript.Meta.Name;
    }

    public void Load(MutableCharacter loadedCharacter, BotcScript loadedScript)
    {
        LoadedScript = loadedScript;
        LoadedCharacter = loadedCharacter;
        CharacterNameTextBox.Text = loadedCharacter.Name;
        CharacterAbilityTextBox.Text = loadedCharacter.Ability;
        CharacterFlavorTextBox.Text = loadedCharacter.Flavor;
        CharacterTeamComboBox.SelectedItem = loadedCharacter.Team;
        EditionTextBox.Text = loadedCharacter.Edition;
        SortComboBox.SelectedItem = new SortEnumBox(loadedCharacter.SortInfo);
        UpdateSetup();
        BagOptionsComboBox.SelectedIndex = (int)loadedCharacter.MutableAppFeatures.Selection;
        LoadedCharacter.PropertyChanged += LoadedCharacter_PropertyChanged;
        LoadedCharacter.MutableAppFeatures.PropertyChanged += MutableAppFeaturesPropertyChanged;
        BagOptionsComboBox.SelectionChanged += BagOptionsComboBox_OnSelectionChanged;
    }

    private void MutableAppFeaturesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedCharacter.MutableAppFeatures.Selection):
                BagOptionsComboBox.SelectedIndex = (int)LoadedCharacter.MutableAppFeatures.Selection;
                break;
        }
    }

    private void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedCharacter.Name):
                CharacterNameTextBox.Text = LoadedCharacter.Name;
                break;
            case nameof(LoadedCharacter.Ability):
            case nameof(LoadedCharacter.SortInfo):
                CharacterAbilityTextBox.Text = LoadedCharacter.Ability;
                SortComboBox.SelectedItem = LoadedCharacter.SortInfo;
                UpdateSetup();
                break;
            case nameof(LoadedCharacter.Flavor):
                CharacterFlavorTextBox.Text = LoadedCharacter.Flavor;
                break;
            case nameof(LoadedCharacter.Team):
                CharacterTeamComboBox.SelectedItem = LoadedCharacter.Team;
                break;
            case nameof(LoadedCharacter.Edition):
                EditionTextBox.Text = LoadedCharacter.Edition;
                break;
        }
    }

    public void Delete()
    {
        LoadedCharacter.PropertyChanged -= LoadedCharacter_PropertyChanged;
    }

    private void BagOptionsComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BagOptionsComboBox.SelectedIndex == -1)
        {
            return;
        }
        LoadedCharacter.MutableAppFeatures.Selection = (SelectionRule)BagOptionsComboBox.SelectedIndex;
    }
}