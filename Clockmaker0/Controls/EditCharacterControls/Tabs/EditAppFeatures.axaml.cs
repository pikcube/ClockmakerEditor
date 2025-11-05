using Avalonia.Controls;
using Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

/// <inheritdoc />
public partial class EditAppFeatures : UserControl
{

    private GeneralAppFeatures EditGeneralAppFeatures { get; }
    private NightSignalFeatures EditNightSignalFeatures { get; }
    private VotingFeatures EditVotingFeatures { get; }
    private GrimRevealFeatures EditGrimRevealFeatures { get; }

    /// <inheritdoc />
    public EditAppFeatures()
    {
        InitializeComponent();
        EditGeneralAppFeatures = new GeneralAppFeatures();
        EditNightSignalFeatures = new NightSignalFeatures();
        EditVotingFeatures = new VotingFeatures();
        EditGrimRevealFeatures = new GrimRevealFeatures();
    }

    /// <summary>
    /// Load the character data into the EditAppFeatures control
    /// </summary>
    /// <param name="loadedCharacter">The character to load</param>
    public void Load(MutableCharacter loadedCharacter)
    {
        EditGeneralAppFeatures.Load(loadedCharacter.MutableAppFeatures);
        EditNightSignalFeatures.Load(loadedCharacter);
        EditVotingFeatures.Load(loadedCharacter.MutableAppFeatures);
        EditGrimRevealFeatures.Load(loadedCharacter);
        LoadedMenuListBox.SelectedIndex = 0;
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        switch (LoadedMenuListBox.SelectedIndex)
        {
            case 0:
                LoadedMenuItemPanel.Children.Clear();
                LoadedMenuItemPanel.Children.Add(EditGeneralAppFeatures);
                return;
            case 1:
                LoadedMenuItemPanel.Children.Clear();
                LoadedMenuItemPanel.Children.Add(EditNightSignalFeatures);
                return;
            case 2:
                LoadedMenuItemPanel.Children.Clear();
                LoadedMenuItemPanel.Children.Add(EditVotingFeatures);
                return;
            case 3:
                LoadedMenuItemPanel.Children.Clear();
                LoadedMenuItemPanel.Children.Add(EditGrimRevealFeatures);
                return;
            default:
                return;
        }
    }

    /// <summary>
    /// Disable editting for read only characters
    /// </summary>
    public void Lock()
    {
        EditGeneralAppFeatures.Lock();
        EditNightSignalFeatures.Lock();
        EditVotingFeatures.IsEnabled = false;
        EditGrimRevealFeatures.IsEnabled = false;
    }

    /// <summary>
    /// Enable editting for non readonly characters
    /// </summary>
    public void Unlock()
    {
        EditGeneralAppFeatures.Unlock();
        EditNightSignalFeatures.Unlock();
        EditVotingFeatures.IsEnabled = true;
        EditGrimRevealFeatures.IsEnabled = true;
    }
}