using Avalonia.Controls;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Core.Special;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

public partial class GeneralAppFeatures : UserControl
{
    public GeneralAppFeatures()
    {
        InitializeComponent();
        Panel.Spacing = 10;
    }

    public void Load(MutableAppFeatures features)
    {
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

    public void Lock()
    {
        DistributeRolesFeature.Lock();
        GhostVotesFeature.Lock();
        PointingFeature.Lock();
        SignalGrimorieFeature.Lock();
        OpenEyesFeature.Lock();
    }

    public void Unlock()
    {
        DistributeRolesFeature.Unlock();
        GhostVotesFeature.Unlock();
        PointingFeature.Unlock();
        SignalGrimorieFeature.Unlock();
        OpenEyesFeature.Unlock();
    }
}