using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public abstract partial class AbstractNightOrder : UserControl, IDelete
{
    protected MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;
    protected MutableBotcScript LoadedScript { get; private set; } = BotcScript.Default.ToMutable();
    private TrackedList<MutableCharacter> LoadedList { get; set; } = [];

    protected abstract string? GetReminder(MutableCharacter character);

    protected abstract void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e);

    protected abstract void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e);

    protected abstract double? GetNightIndex(MutableCharacter mutableCharacter);

    protected abstract void SetNightIndex(MutableCharacter loadedCharacter, double? i);

    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPop;
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;

    public AbstractNightOrder()
    {
        InitializeComponent();
    }

    protected void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader, bool isChecked, TrackedList<MutableCharacter> loadedList, string? initialReminderText)
    {
        LoadedCharacter = loadedCharacter;
        LoadedScript = loadedScript;
        LoadedList = loadedList;

        IsAbstractNightEnabledCheckBox.IsChecked = isChecked;
        AbstractNightReminderTextBox.IsEnabled = IsAbstractNightEnabledCheckBox.IsChecked is true;
        AbstractNightReminderTextBox.Text = initialReminderText;

        AbstractNightOrderView.Load(loadedList, loadedScript, loader, GetReminder);
        AbstractNightOrderView.OnPop += AbstractNightOrderView_OnPop;
        AbstractNightOrderView.OnDelete += AbstractNightOrderView_OnDelete;

        LoadedCharacter.PropertyChanged += LoadedCharacter_PropertyChanged;
        LoadedCharacter.OnDelete += LoadedCharacter_OnDelete;
        AbstractNightReminderTextBox.TextChanged += AbstractNightReminderTextBox_OnTextChanged;
        IsAbstractNightEnabledCheckBox.IsCheckedChanged += IsAbstractNightEnabledCheckBox_OnIsCheckedChanged;
    }

    private void AbstractNightOrderView_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(sender, e);
    }

    private void AbstractNightOrderView_OnPop(object? sender, SimpleEventArgs<UserControl, string> e)
    {
        OnPop?.Invoke(sender, e);
    }

    private void LoadedCharacter_OnDelete(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        Delete();
    }

    public void Lock()
    {
        IsAbstractNightEnabledCheckBox.IsEnabled = false;
        AbstractNightReminderTextBox.IsEnabled = false;
    }

    public void Unlock()
    {
        IsAbstractNightEnabledCheckBox.IsEnabled = true;
        AbstractNightReminderTextBox.IsEnabled = IsAbstractNightEnabledCheckBox.IsChecked is true;
    }

    public void Delete()
    {
        LoadedCharacter.PropertyChanged -= LoadedCharacter_PropertyChanged;
        LoadedCharacter.OnDelete -= LoadedCharacter_OnDelete;
        AbstractNightOrderView.Delete();

    }

    private void IsAbstractNightEnabledCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        AbstractNightReminderTextBox.IsEnabled = IsAbstractNightEnabledCheckBox.IsChecked is true;
        switch (IsAbstractNightEnabledCheckBox.IsChecked, LoadedList.Contains(LoadedCharacter))
        {
            case (true, true):
            case (false, false):
                return;
            case (false, true):
                LoadedList.Remove(LoadedCharacter);
                return;
            case (true, false):
                if (GetNightIndex(LoadedCharacter) < 1)
                {
                    SetNightIndex(LoadedCharacter, 1000);
                }
                int index = LoadedList.FindIndex(c => GetNightIndex(c) >= GetNightIndex(LoadedCharacter));
                if (index == -1)
                {
                    LoadedList.Add(LoadedCharacter);
                }
                else
                {
                    LoadedList.Insert(index, LoadedCharacter);
                }
                return;
        }
    }
}