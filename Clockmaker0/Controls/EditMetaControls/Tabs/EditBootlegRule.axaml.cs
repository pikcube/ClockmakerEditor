using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditMetaControls.Tabs;

/// <inheritdoc />
public partial class EditBootlegRule : UserControl
{
    private BootlegRule LoadedRule { get; set; } = new("loading...");

    /// <summary>
    /// Raised when this control is to delete itself. The creator is responsible for detatching this control from the visual tree
    /// </summary>
    public event EventHandler<EditBootlegRule>? OnDelete;

    /// <inheritdoc />
    public EditBootlegRule()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the specified bootlegger control into the current object
    /// </summary>
    /// <param name="rule">The rule to load</param>
    public void Load(BootlegRule rule)
    {
        LoadedRule = rule;

        BootlegTextBox.Text = LoadedRule.Rule;

        LoadedRule.PropertyChanged += LoadedRule_PropertyChanged;
        BootlegTextBox.TextChanged += BootlegTextBox_OnTextChanged;
        DeleteButton.Click += DeleteButton_OnClick;
    }

    private void LoadedRule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedRule.Rule):
                BootlegTextBox.Text = LoadedRule.Rule;
                break;
        }
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (TopLevel.GetTopLevel(this) is not Window top)
            {
                return;
            }
            if (!App.IsKeyDown(Key.RightShift, Key.LeftShift))
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    "Are you sure you want to delete this bootleg rule?",
                    ButtonEnum.YesNo).ShowWindowDialogAsync(top);
                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }


            LoadedRule.Delete();
            OnDelete?.Invoke(this, this);
        });
    }

    private void BootlegTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedRule.Rule = BootlegTextBox.Text ?? "";
    }
}