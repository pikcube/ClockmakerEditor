using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

public partial class NightSignalItem : UserControl
{
    public MutableSignal LoadedSignal { get; set; } = new("");
    public NightSignalItem()
    {
        InitializeComponent();
    }

    public void Load(MutableSignal signal)
    {
        LoadedSignal = signal;

        SignalTextBox.Text = signal.Value;
        if (signal.Direction == DirectionEnum.None)
        {
            signal.Direction = DirectionEnum.Card;
        }

        TimeScopeGrid.ShowHideColumn(0, false);
        TimeScopeGrid.ShowHideColumn(2, false);
        TimeScopeGrid.ShowHideColumn(4, false);

        DirectionComboBox.SelectedIndex = (int)signal.Direction - 1;
        TimeScopeGrid.Load(signal.CardTimes);
        ExpandButton.Click += ExpandButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        TimeScopeGrid.CheckBoxChanged += TimeScopeGrid_CheckBoxChanged;
        SignalTextBox.TextChanged += SignalTextBox_TextChanged;
        DirectionComboBox.SelectionChanged += DirectionComboBox_SelectionChanged;
        LoadedSignal.PropertyChanged += LoadedSignal_PropertyChanged;
    }

    private void DirectionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadedSignal.Direction = DirectionComboBox.SelectedIndex switch
        {
            0 => DirectionEnum.Card,
            1 => DirectionEnum.Player,
            _ => DirectionEnum.None
        };
    }

    private void SignalTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedSignal.Value = SignalTextBox.Text ?? string.Empty;
    }

    private void LoadedSignal_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedSignal.CardTimes):
                TimeScopeGrid.TryUpdateGrid(LoadedSignal.CardTimes);
                break;
        }
    }

    private void TimeScopeGrid_CheckBoxChanged(object? sender, TimeScopeGrid.GridEventArgs e)
    {
        if (e.IsSet)
        {
            LoadedSignal.CardTimes.SetFlag(e.Scope, e.FlagChanged);
        }
        else
        {
            LoadedSignal.CardTimes.ClearFlag(e.Scope, e.FlagChanged);
        }
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadedSignal.Delete();
    }

    private void ExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        TimeScopeGrid.IsVisible = !TimeScopeGrid.IsVisible;
        DeleteButton.IsVisible = !DeleteButton.IsVisible;
    }

    public void Lock()
    {
        SignalTextBox.IsEnabled = false;
        DirectionComboBox.IsEnabled = false;
        TimeScopeGrid.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    public void Unlock()
    {
        SignalTextBox.IsEnabled = true;
        DirectionComboBox.IsEnabled = true;
        TimeScopeGrid.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }
}