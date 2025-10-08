using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

public partial class VotingFeatures : UserControl
{
    private MutableAppFeatures LoadedAppFeatures { get; set; } = new(MutableCharacter.Default);

    public VotingFeatures()
    {
        InitializeComponent();
    }

    public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript)
    {
        LoadedAppFeatures = loadedCharacter.MutableAppFeatures;
        MultiplierPicker.ParsingNumberStyle = NumberStyles.Integer;
        MultiplierPicker.Value = LoadedAppFeatures.Multiplier;
        MultiplierPicker.ValueChanged += MultiplierPicker_ValueChanged;
        HiddenVoteComboBox.SelectedIndex = LoadedAppFeatures.IsHidden ? 1 : 0;
        HiddenVoteComboBox.SelectionChanged += HiddenVoteComboBox_SelectionChanged;
        LoadedAppFeatures.PropertyChanged += AppFeaturesPropertyChanged;
    }

    private void HiddenVoteComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        switch (HiddenVoteComboBox.SelectedIndex)
        {
            case 0:
                LoadedAppFeatures.IsHidden = false;
                break;
            case 1:
                LoadedAppFeatures.IsHidden = true;
                break;
            default:
                HiddenVoteComboBox.SelectedIndex = LoadedAppFeatures.IsHidden ? 1 : 0;
                break;
        }
    }

    private void AppFeaturesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedAppFeatures.Multiplier):
                MultiplierPicker.Value = LoadedAppFeatures.Multiplier;
                break;
            case nameof(LoadedAppFeatures.IsHidden):
                HiddenVoteComboBox.SelectedIndex = LoadedAppFeatures.IsHidden ? 1 : 0;
                break;
        }
    }

    private void MultiplierPicker_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (MultiplierPicker.Value is null)
        {
            MultiplierPicker.Value = 1;
            return;
        }



        LoadedAppFeatures.Multiplier = decimal.ToInt32(MultiplierPicker.Value.Value);
    }
}