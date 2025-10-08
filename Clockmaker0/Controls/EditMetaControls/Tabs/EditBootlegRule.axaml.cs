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

public partial class EditBootlegRule : UserControl
{
    private BootlegRule LoadedRule { get; set; } = new("loading...");
    private Action DeleteControl { get; set; } = () => { };

    public EditBootlegRule()
    {
        InitializeComponent();
    }

    public void Load(BootlegRule rule, Action deleteAction)
    {
        LoadedRule = rule;
        DeleteControl = deleteAction;

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
            if (!App.IsKeyDown(Key.RightShift, Key.LeftShift))
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    "Are you sure you want to delete this bootleg rule?",
                    ButtonEnum.YesNo).ShowWindowDialogAsync(App.MainWindow);
                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }


            LoadedRule.Delete();
            DeleteControl();
        });
    }

    private void BootlegTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedRule.Rule = BootlegTextBox.Text ?? "";
    }
}