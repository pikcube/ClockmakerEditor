using Avalonia.Controls;
using Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class EditAppFeatures : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    private GeneralAppFeatures EditGeneralAppFeatures { get; }
    private NightSignalFeatures EditNightSignalFeatures { get; }
    private VotingFeatures EditVotingFeatures { get; }
    private GrimRevealFeatures EditGrimRevealFeatures { get; }

    public EditAppFeatures()
    {
        InitializeComponent();
        EditGeneralAppFeatures = new GeneralAppFeatures();
        EditNightSignalFeatures = new NightSignalFeatures();
        EditVotingFeatures = new VotingFeatures();
        EditGrimRevealFeatures = new GrimRevealFeatures();
    }

    public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript)
    {
        LoadedCharacter = loadedCharacter;
        LoadedScript = loadedScript;
        EditGeneralAppFeatures.Load(loadedCharacter.MutableAppFeatures);
        EditNightSignalFeatures.Load(loadedCharacter);
        EditVotingFeatures.Load(loadedCharacter, loadedScript);
        EditGrimRevealFeatures.Load(loadedCharacter);
        LoadedMenuListBox.SelectedIndex = 0;
    }


    public void Delete()
    {

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

    public void Lock()
    {
        EditGeneralAppFeatures.Lock();
        EditNightSignalFeatures.Lock();
        EditVotingFeatures.IsEnabled = false;
        EditGrimRevealFeatures.IsEnabled = false;
    }

    public void Unlock()
    {
        EditGeneralAppFeatures.Unlock();
        EditNightSignalFeatures.Unlock();
        EditVotingFeatures.IsEnabled = true;
        EditGrimRevealFeatures.IsEnabled = true;
    }
}