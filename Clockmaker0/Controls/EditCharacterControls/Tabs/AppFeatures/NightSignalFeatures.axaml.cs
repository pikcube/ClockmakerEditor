using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// Control for creating Night Signals in the app
/// </summary>
public partial class NightSignalFeatures : UserControl, ILock
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;

    private Button NewButton { get; set; }

    private List<NightSignalItem> NightSignalItems { get; } = [];

    /// <inheritdoc />
    public NightSignalFeatures()
    {
        InitializeComponent();
        NewButton = new Button
        {
            Content = "Add Signal",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
    }

    /// <summary>
    /// Load in the current character data
    /// </summary>
    /// <param name="loadedCharacter">The character to load</param>
    public void Load(MutableCharacter loadedCharacter)
    {
        LoadedCharacter = loadedCharacter;
        NewButton.Click += NewButton_Click;
        NightSignalStack.Children.Add(NewButton);
        foreach (MutableSignal signal in loadedCharacter.MutableAppFeatures.Signals)
        {
            NightSignalItem ni = new();
            ni.Load(signal);
            NightSignalItems.Add(ni);
            NightSignalStack.Children.Add(ni);
        }

        LoadedCharacter.MutableAppFeatures.Signals.ItemAdded += Signals_ItemAdded;
        LoadedCharacter.MutableAppFeatures.Signals.ItemRemoved += Signals_ItemRemoved;
    }

    private void Signals_ItemRemoved(object? sender, ValueChangedArgs<MutableSignal> e)
    {
        NightSignalItem? nsi = NightSignalStack.Children.OfType<NightSignalItem>().FirstOrDefault(nsi => nsi.LoadedSignal == e.NewValue);
        if (nsi is null)
        {
            return;
        }

        NightSignalItems.Remove(nsi);
        NightSignalStack.Children.Remove(nsi);
    }

    private void Signals_ItemAdded(object? sender, ValueChangedArgs<MutableSignal> e)
    {
        NightSignalItem ni = new();
        ni.Load(e.NewValue);
        NightSignalItems.Add(ni);
        NightSignalStack.Children.Add(ni);
    }

    private void NewButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadedCharacter.MutableAppFeatures.Signals.Add(new MutableSignal("", true));
    }

    /// <inheritdoc />
    public void Lock()
    {
        NewButton.IsEnabled = false;
        foreach (NightSignalItem nsi in NightSignalItems)
        {
            nsi.Lock();
        }
    }

    /// <inheritdoc />
    public void Unlock()
    {
        NewButton.IsEnabled = true;

        foreach (NightSignalItem nsi in NightSignalItems)
        {
            nsi.Unlock();
        }
    }
}