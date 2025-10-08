using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class EditReminderToken : UserControl, IDelete
{
    private ReminderToken? Token { get; set; }
    private Func<UserControl, bool>? OnDelete { get; set; }
    public EditReminderToken()
    {
        InitializeComponent();
    }


    public void Load(ReminderToken token, Func<UserControl, bool> deleteAction)
    {
        Token = token;
        ReminderTextBox.Text = Token.Text;
        IsGlobalComboBox.SelectedIndex = Token.IsGlobal ? 1 : 0;

        Token.TextChanged += Token_TextChanged;
        Token.IsGlobalChanged += Token_IsGlobalChanged;
        OnDelete = deleteAction;

        Token.OnDelete += Token_OnDelete;
    }

    private void Token_OnDelete(object? sender, ValueChangedArgs<ReminderToken> e)
    {
        Delete();
    }

    private void Token_IsGlobalChanged(object? sender, ValueChangedArgs<bool> e)
    {
        IsGlobalComboBox.SelectedIndex = e.NewValue ? 1 : 0;
    }

    private void Token_TextChanged(object? sender, ValueChangedArgs<string> e)
    {
        ReminderTextBox.Text = e.NewValue;
    }

    private void ReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Token is null)
        {
            return;
        }
        Token.Text = ReminderTextBox.Text ?? Token.Text;
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (!App.IsKeyDown(Key.RightShift, Key.LeftShift))
            {
                IMsBox<ButtonResult> msg = MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    "Are you sure you want to delete this reminder?", ButtonEnum.YesNo);

                ButtonResult result = TopLevel.GetTopLevel(this) is Window window
                    ? await msg.ShowWindowDialogAsync(window)
                    : await msg.ShowAsPopupAsync(this);

                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }

            Token?.Delete();
        });
    }

    private void IsGlobalComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Token is null)
        {
            return;
        }

        Token.IsGlobal = IsGlobalComboBox.SelectedIndex == 1;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
    }

    public void Delete()
    {
        IsVisible = false;
        IsEnabled = false;
        IsGlobalComboBox.SelectionChanged -= IsGlobalComboBox_OnSelectionChanged;
        ReminderTextBox.TextChanged -= ReminderTextBox_OnTextChanged;
        OnDelete?.Invoke(this);
    }
}