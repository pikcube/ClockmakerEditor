using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;

namespace Clockmaker0.Controls.CharacterImport;

/// <summary>
/// Control for importing a character from a known repo
/// </summary>
public partial class OfficialCharacterImport : Window
{
    private OfficialCharacterPreview[] _officialCharacterPreviews = [];
    /// <summary>
    /// True when the import button is clicked, so the caller knows whether or not to honor the selections
    /// </summary>
    public bool IsConfirmed { get; private set; }

    /// <inheritdoc />
    public OfficialCharacterImport()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the characters to be selected for import
    /// </summary>
    /// <param name="scriptImageLoader">The script's image loader</param>
    /// <param name="source">The characters to load</param>
    public void Load(ScriptImageLoader scriptImageLoader, IEnumerable<Character> source)
    {
        _officialCharacterPreviews =
        [
            ..source.Where(c => c.Team != TeamEnum.Special).Select(c => new OfficialCharacterPreview().Load(c, scriptImageLoader))
        ];

        foreach (OfficialCharacterPreview ocp in _officialCharacterPreviews)
        {
            ocp.ImportCheckBox.IsCheckedChanged += ImportCheckBox_IsCheckedChanged;
        }

        ResultPanel.ItemsSource = _officialCharacterPreviews;
        ResultPanel.SelectionMode = (SelectionMode)3;
        ResultPanel.SelectionChanged += ResultPanel_SelectionChanged;
        UpdateView();
    }

    private void ImportCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        foreach (OfficialCharacterPreview ocp in _officialCharacterPreviews)
        {
            switch (ocp.IsImported)
            {
                case true when ResultPanel.SelectedItems?.Contains(ocp) is false:
                    {
                        if (ResultPanel.SelectedItems is null)
                        {
                            ResultPanel.SelectedItems = new List<OfficialCharacterPreview> { ocp };
                        }
                        else
                        {
                            ResultPanel.SelectedItems.Add(ocp);
                        }

                        break;
                    }
                case false when ResultPanel.SelectedItems?.Contains(ocp) is true:
                    ResultPanel.SelectedItems.Remove(ocp);
                    break;
            }
        }
    }

    private void ResultPanel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (OfficialCharacterPreview ocp in _officialCharacterPreviews)
        {
            ocp.ImportCheckBox.IsChecked = ResultPanel.SelectedItems?.Contains(ocp);
        }
    }

    /// <summary>
    /// The characters that are currently selected for import
    /// </summary>
    public IEnumerable<ICharacter> ImportedCharacters => _officialCharacterPreviews.Where(ocp => ocp.IsImported).Select(ocp => ocp.LoadedCharacter);

    private void UpdateView()
    {
        if (string.IsNullOrEmpty(CharacterNameTextBox.Text))
        {
            foreach (OfficialCharacterPreview ocp in _officialCharacterPreviews)
            {
                ocp.IsVisible = true;
                ocp.IsEnabled = true;
            }
        }
        else
        {
            foreach (OfficialCharacterPreview ocp in _officialCharacterPreviews)
            {
                bool toEnable = ocp.IsImported || ocp.LoadedCharacter.Name.Contains(CharacterNameTextBox.Text, StringComparison.CurrentCultureIgnoreCase);
                ocp.IsVisible = toEnable;
                ocp.IsEnabled = toEnable;
            }
        }
        ResultPanel.ItemsSource = _officialCharacterPreviews.Where(ocp => ocp.IsEnabled);
    }

    private void CharacterNameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateView();
    }

    private void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }
}