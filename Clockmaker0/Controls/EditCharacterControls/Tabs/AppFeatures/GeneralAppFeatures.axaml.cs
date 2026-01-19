using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.AddIns;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Core.Special;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// The meta control for editting the generic app features
/// </summary>
public partial class GeneralAppFeatures : UserControl, ILock
{
    /// <inheritdoc />
    public GeneralAppFeatures()
    {
        InitializeComponent();
        Panel.Spacing = 10;
    }

    /// <summary>
    /// Load in the current App Features. May only be called once
    /// </summary>
    /// <param name="features">The features to load</param>
    public void Load(MutableAppFeatures features)
    {
        GoodDuplicatesFeature.Load("Allow Duplicates of Good Roles", "Atheist, Pope", new ReferenceProperty<bool>(() => features.IsGoodDuplicates, features));

        DistributeRolesFeature.Load("Manually Distribute Roles", features.AbilityDistributeRoles, "Example: Gardener", new ScopeTimes { None = TimeOfDay.Pregame });
        DistributeRolesFeature.TimeScopeGrid.ShowHideColumn(2, false);
        DistributeRolesFeature.TimeScopeGrid.ShowHideColumn(3, false);
        DistributeRolesFeature.TimeScopeGrid.ShowHideColumn(4, false);

        GhostVotesFeature.Load("Return Ghost Votes", features.AbilityGhostVotes, "Example: Ferry Man", new ScopeTimes { None = TimeOfDay.All });
        GhostVotesFeature.TimeScopeGrid.ShowHideColumn(0, false);

        PointingFeature.Load("Start Pointing Vote", features.AbilityPointing, "Examples: Boomdandy, Lil Monsta", new ScopeTimes { None = TimeOfDay.All });

        SignalGrimorieFeature.Load("Send Grimoire", features.SignalGrimoire, "Examples: Widow, Spy", new ScopeTimes { None = TimeOfDay.All });
        SignalGrimorieFeature.TimeScopeGrid.ShowHideColumn(0, false);
        SignalGrimorieFeature.TimeScopeGrid.ShowHideColumn(2, false);
        SignalGrimorieFeature.TimeScopeGrid.ShowHideColumn(4, false);

        OpenEyesFeature.Load("Player Open Eyes", features.PlayerOpenEyes, "Example: Wraith", new ScopeTimes { None = TimeOfDay.All });
        OpenEyesFeature.TimeScopeGrid.ShowHideColumn(0, false);
        OpenEyesFeature.TimeScopeGrid.ShowHideColumn(2, false);
        OpenEyesFeature.TimeScopeGrid.ShowHideColumn(4, false);
    }

    /// <inheritdoc />
    public void Lock()
    {
        GoodDuplicatesFeature.Lock();
        DistributeRolesFeature.Lock();
        GhostVotesFeature.Lock();
        PointingFeature.Lock();
        SignalGrimorieFeature.Lock();
        OpenEyesFeature.Lock();
    }

    /// <inheritdoc />
    public void Unlock()
    {
        GoodDuplicatesFeature.Unlock();
        DistributeRolesFeature.Unlock();
        GhostVotesFeature.Unlock();
        PointingFeature.Unlock();
        SignalGrimorieFeature.Unlock();
        OpenEyesFeature.Unlock();
    }
}