using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;
using System.ComponentModel;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs.AppFeatures;

/// <summary>
/// Control for editting a single night signal
/// </summary>
public partial class NightSignalItem : UserControl, ILock
{
    /// <summary>
    /// The currently loaded signal
    /// </summary>
    public MutableSignal LoadedSignal { get; private set; } = new("");

    /// <inheritdoc />
    public NightSignalItem()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the siganl data. May only be called once
    /// </summary>
    /// <param name="signal">The signal to load</param>
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
        TaskManager.ScheduleTask(async () =>
        {
            if (!App.IsKeyDown(Key.RightShift, Key.LeftShift))
            {
                MessageBoxStandardParams messageBoxStandardParams = new()
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentTitle = "Confirm Delete",
                    ContentMessage = "Are you sure you want to delete this signal?",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SizeToContent = SizeToContent.Width
                };
                IMsBox<ButtonResult> msg = MessageBoxManager.GetMessageBoxStandard(messageBoxStandardParams);


                ButtonResult result = TopLevel.GetTopLevel(this) is Window window
                    ? await msg.ShowWindowDialogAsync(window)
                    : await msg.ShowAsPopupAsync(this);

                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }

            LoadedSignal.Delete();
        });
    }

    private void ExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        TimeScopeGrid.IsVisible = !TimeScopeGrid.IsVisible;
    }

    /// <inheritdoc />
    public void Lock()
    {
        SignalTextBox.IsEnabled = false;
        DirectionComboBox.IsEnabled = false;
        TimeScopeGrid.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    /// <inheritdoc />
    public void Unlock()
    {
        SignalTextBox.IsEnabled = true;
        DirectionComboBox.IsEnabled = true;
        TimeScopeGrid.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }
}