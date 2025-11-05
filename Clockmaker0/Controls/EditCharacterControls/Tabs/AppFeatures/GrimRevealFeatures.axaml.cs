using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// Control for enabling swap in grimoire
/// </summary>
public partial class GrimRevealFeatures : UserControl
{
    private MutableAppFeatures LoadedAppFeatures { get; set; } = new(MutableCharacter.Default);

    /// <inheritdoc />
    public GrimRevealFeatures()
    {
        InitializeComponent();
        DescriptionTextBlock.Text = """
                                    For characters that think they are other characters or gain the ability of other characters. Marking a character with this reminder token will automatically swap their character in the grimoire during the grim reveal.
                                    
                                    Examples: Drunk, Lunatic, Philosopher, Marionette
                                    """;
    }

    /// <summary>
    /// Load the character in, may only be called once
    /// </summary>
    /// <param name="loadedCharacter">The character to load</param>
    public void Load(MutableCharacter loadedCharacter)
    {
        LoadedAppFeatures = loadedCharacter.MutableAppFeatures;
        if (LoadedAppFeatures.IsSwapRevealToken)
        {
            LoadedAppFeatures.SwapRevealToken ??= LoadedAppFeatures.CreateFirstGlobal();
            IsRevealEnabledCheckBox.IsChecked = true;
            ReminderTokenTextBox.Text = LoadedAppFeatures.SwapRevealToken;
        }
        else
        {
            IsRevealEnabledCheckBox.IsChecked = false;
            ReminderTokenTextBox.Text = null;
            ReminderTokenTextBox.IsEnabled = false;
        }

        IsRevealEnabledCheckBox.IsCheckedChanged += IsRevealEnabledCheckBox_IsCheckedChanged;
        ReminderTokenTextBox.TextChanged += ReminderTokenTextBox_TextChanged;
        LoadedAppFeatures.PropertyChanged += LoadedAppFeatures_PropertyChanged;
    }

    private void LoadedAppFeatures_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedAppFeatures.SwapRevealToken):
                ReminderTokenTextBox.Text = LoadedAppFeatures.SwapRevealToken;
                break;
            case nameof(LoadedAppFeatures.IsSwapRevealToken):
                IsRevealEnabledCheckBox.IsChecked = LoadedAppFeatures.IsSwapRevealToken;
                break;
        }
    }

    private void ReminderTokenTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedAppFeatures.SwapRevealToken = ReminderTokenTextBox.Text;
    }

    private void IsRevealEnabledCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ReminderTokenTextBox.IsEnabled = IsRevealEnabledCheckBox.IsChecked is true;
        LoadedAppFeatures.IsSwapRevealToken = IsRevealEnabledCheckBox.IsChecked is true;
    }
}