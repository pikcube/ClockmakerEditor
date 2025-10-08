using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditMetaControls.Tabs;

public partial class BootlegRulesTab : UserControl
{
    public MutableMeta LoadedScript { get; set; } = MutableMeta.Default;

    private TrackedList<BootlegRule> Rules { get; set; } = [];

    public BootlegRulesTab()
    {
        InitializeComponent();
    }

    public void Load(MutableMeta loadMeta)
    {
        LoadedScript = loadMeta;
        Rules = loadMeta.Bootlegger;

        BootlegStack.Children.AddRange(Rules.Select(z =>
        {
            EditBootlegRule ebr = new();
            ebr.Load(z, () => BootlegStack.Children.Remove(ebr));
            return ebr;
        }));

        Button button = new()
        {
            Content = "Add Bootleg",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        button.Click += ButtonControl_Click;
        BootlegStack.Children.Add(button);
    }

    private void ButtonControl_Click(object? sender, RoutedEventArgs e)
    {
        BootlegRule bootlegRule = new("");
        Rules.Add(bootlegRule);
        EditBootlegRule ebr = new();
        ebr.Load(bootlegRule, () => BootlegStack.Children.Remove(ebr));
        BootlegStack.Children.Insert(BootlegStack.Children.Count - 1, ebr);
    }
}