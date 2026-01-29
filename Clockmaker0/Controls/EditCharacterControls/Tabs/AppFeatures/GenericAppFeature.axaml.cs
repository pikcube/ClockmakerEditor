using System.ComponentModel;
using System.Data;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.AddIns;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// Control that modifies a single app feature
/// </summary>
public partial class GenericAppFeature : UserControl, ILock
{
    private ScopeTimes LoadedFeatureScopeTimes { get; set; } = new();
    private ScopeTimes DefaultScopeTimes { get; set; } = new();
    private ReferenceProperty<bool>? RefBool { get; set; }

    /// <inheritdoc />
    public GenericAppFeature()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the current generic app feature data
    /// </summary>
    /// <param name="description">Explanation</param>
    /// <param name="example">An example of a character with this feature</param>
    /// <param name="referenceProperty">Function returning the property to update</param>
    public void Load(string description, string example, ReferenceProperty<bool> referenceProperty)
    {
        DescriptionTextBlock.Text = description;
        ExampleTextBlock.Text = example;
        RefBool = referenceProperty;


        TimeScopeGrid.IsVisible = false;
        ExpandButton.IsVisible = false;
        TimeScopeGrid.IsEnabled = false;
        ExpandButton.IsEnabled = false;
        IsEnabledComboBox.SelectedIndex = RefBool ? 1 : 0;

        IsEnabledComboBox.SelectionChanged += IsEnabledComboBox_BindedBoolean_SelectionChanged;
    }

    /// <summary>
    /// Called when the underlying reference property updates to a new value
    /// </summary>
    /// <exception cref="NoNullAllowedException">Thrown if object was not initialized with a reference property</exception>
    public void OnReferencePropertyChanged()
    {
        if (RefBool is null)
        {
            throw new NoNullAllowedException();
        }
        IsEnabledComboBox.SelectedIndex = RefBool ? 1 : 0;
    }

    private void IsEnabledComboBox_BindedBoolean_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RefBool is null)
        {
            throw new NoNullAllowedException();
        }

        RefBool.Value = IsEnabledComboBox.SelectedIndex == 1;
    }

    /// <summary>
    /// Load the current generic app feature data
    /// </summary>
    /// <param name="description">Explanation</param>
    /// <param name="times">When the feature is enabled</param>
    /// <param name="example">An example of a character with this feature</param>
    /// <param name="defaultTimes">The default times to set if the feature is enabled without setting a time</param>
    public void Load(string description, ScopeTimes times, string example, ScopeTimes defaultTimes)
    {
        DescriptionTextBlock.Text = description;
        LoadedFeatureScopeTimes = times;
        ExampleTextBlock.Text = example;
        DefaultScopeTimes = defaultTimes;

        TimeScopeGrid.Load(LoadedFeatureScopeTimes);

        IsEnabledComboBox.SelectedIndex = LoadedFeatureScopeTimes.GetPairs(true).Any() ? 1 : 0;

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


        IsEnabledComboBox.SelectedIndex = LoadedFeatureScopeTimes.GetPairs(true).Any() ? 1 : 0;
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

    /// <inheritdoc />
    public void Lock()
    {
        TimeScopeGrid.IsEnabled = false;
        IsEnabledComboBox.IsEnabled = false;
    }

    /// <inheritdoc />
    public void Unlock()
    {
        TimeScopeGrid.IsEnabled = true;
        IsEnabledComboBox.IsEnabled = true;
    }
}