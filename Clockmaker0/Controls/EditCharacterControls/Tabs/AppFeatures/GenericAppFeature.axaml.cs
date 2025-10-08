using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

public partial class GenericAppFeature : UserControl
{
    public ScopeTimes LoadedFeatureScopeTimes { get; set; } = new();
    public ScopeTimes DefaultScopeTimes { get; set; } = new();

    public GenericAppFeature()
    {
        InitializeComponent();
    }

    public void Load(string description, ScopeTimes times, string example, ScopeTimes defaultTimes)
    {
        DescriptionTextBlock.Text = description;
        LoadedFeatureScopeTimes = times;
        ExampleTextBlock.Text = example;
        DefaultScopeTimes = defaultTimes;

        TimeScopeGrid.Load(LoadedFeatureScopeTimes);

        if (LoadedFeatureScopeTimes.GetPairs(true).Any())
        {
            IsEnabledComboBox.SelectedIndex = 1;
        }
        else
        {
            IsEnabledComboBox.SelectedIndex = 0;
        }

        ExpandButton.Click += ExpandButton_Click;
        TimeScopeGrid.CheckBoxChanged += TimeScopeGrid_CheckBoxChanged;
        IsEnabledComboBox.SelectionChanged += IsEnabledComboBox_SelectionChanged;
        LoadedFeatureScopeTimes.PropertyChanged += LoadedFeatureScopeTimes_PropertyChanged;
    }

    private void ExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        TimeScopeGrid.IsVisible = !TimeScopeGrid.IsVisible;
    }

    private void TimeScopeGrid_CheckBoxChanged(object? sender, TimeScopeGrid.GridEventArgs e)
    {
        if (e.IsSet)
        {
            LoadedFeatureScopeTimes.SetFlag(e.Scope, e.FlagChanged);
        }
        else
        {
            LoadedFeatureScopeTimes.ClearFlag(e.Scope, e.FlagChanged);
        }


        if (LoadedFeatureScopeTimes.GetPairs(true).Any())
        {
            IsEnabledComboBox.SelectedIndex = 1;
        }
        else
        {
            IsEnabledComboBox.SelectedIndex = 0;
        }
    }

    private void LoadedFeatureScopeTimes_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TimeScopeGrid.TryUpdateGrid(LoadedFeatureScopeTimes);
    }

    private void IsEnabledComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        switch (IsEnabledComboBox.SelectedIndex)
        {
            case 0:
                new ScopeTimes().CopyTo(LoadedFeatureScopeTimes);
                break;
            case 1:
                if (LoadedFeatureScopeTimes.GetPairs(true).Any())
                {
                    break;
                }

                DefaultScopeTimes.CopyTo(LoadedFeatureScopeTimes);
                break;
        }
    }

    public void Lock()
    {
        TimeScopeGrid.IsEnabled = false;
        IsEnabledComboBox.IsEnabled = false;
    }

    public void Unlock()
    {
        TimeScopeGrid.IsEnabled = true;
        IsEnabledComboBox.IsEnabled = true;
    }
}