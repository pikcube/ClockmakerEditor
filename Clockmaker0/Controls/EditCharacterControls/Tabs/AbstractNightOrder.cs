using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;
using System;
using System.ComponentModel;
using Avalonia.Media;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs
{
    /// <summary>
    /// An incomplete implementation of the night order control
    /// </summary>
    public abstract class AbstractNightOrder : UserControl, IDelete, IOnPop, IOnDelete
    {
        /// <summary>
        /// The Currently Loaded Character
        /// </summary>
        protected MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;
        private TrackedList<MutableCharacter> LoadedList { get; set; } = [];

        /// <summary>
        /// Get the night reminder text associated with the character. Override to switch between first and other night
        /// </summary>
        /// <param name="character">The character to fetch</param>
        /// <returns>The night order text to display</returns>
        protected abstract string? GetReminder(MutableCharacter character);

        /// <summary>
        /// Respond to any property on the loaded character changed. Respond by updating the character's reminder textbox
        /// </summary>
        /// <param name="sender">The loaded character (boxxed)</param>
        /// <param name="e">The property changed info</param>
        protected abstract void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e);

        /// <summary>
        /// Raised when the text in the reminder text box is changed. Respond by updating the reminder property in the Loaded Character
        /// </summary>
        /// <param name="sender">The loaded character (boxxed)</param>
        /// <param name="e">The text changed info</param>
        protected abstract void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e);
        /// <summary>
        /// The night indexes for the time of day. Override to switch between first night and other night.
        /// </summary>
        protected abstract string[] NightIndexes { get; }

        /// <inheritdoc />
        public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPop;

        /// <inheritdoc />
        public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;

        /// <summary>
        /// Mark whether or not this character is included in the night order
        /// </summary>
        protected CheckBox IsAbstractNightEnabledCheckBox { get; }
        /// <summary>
        /// The current night order text
        /// </summary>
        protected TextBox AbstractNightReminderTextBox { get; }
        private NightOrderView AbstractNightOrderView { get; }

        /// <inheritdoc />
        protected AbstractNightOrder()
        {
            ItemsControl itemsControl = new();
            Grid newGrid = new()
            {
                RowDefinitions =
                [
                    new RowDefinition(GridLength.Auto),

                ],
                ColumnDefinitions =
                [
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(10)),
                    new ColumnDefinition(GridLength.Star)
                ]
            };
            IsAbstractNightEnabledCheckBox = new CheckBox
            {
                Content = "Abstract Night"
            };
            newGrid.Children.Add(IsAbstractNightEnabledCheckBox);
            Grid.SetRow(IsAbstractNightEnabledCheckBox, 0);
            Grid.SetColumn(IsAbstractNightEnabledCheckBox, 0);
            AbstractNightReminderTextBox = new TextBox
            {
                TextWrapping = TextWrapping.Wrap
            };
            newGrid.Children.Add(AbstractNightReminderTextBox);
            Grid.SetRow(AbstractNightReminderTextBox, 0);
            Grid.SetColumn(AbstractNightReminderTextBox, 2);
            itemsControl.Items.Add(newGrid);
            AbstractNightOrderView = new NightOrderView();
            itemsControl.Items.Add(AbstractNightOrderView);
            Content = itemsControl;
        }

        /// <summary>
        /// Load the control with the supplied character and night order. May only be called once.
        /// </summary>
        /// <param name="loadedCharacter">The character to load</param>
        /// <param name="loadedScript">The script the character is on</param>
        /// <param name="loader">The image loader for the character</param>
        /// <param name="isChecked">True if the character should be currently listed in night order, false otherwise</param>
        /// <param name="loadedList">The current night order</param>
        /// <param name="initialReminderText">The current night reminder text</param>
        protected void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader, bool isChecked, TrackedList<MutableCharacter> loadedList, string? initialReminderText)
        {
            LoadedCharacter = loadedCharacter;
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

        private void AbstractNightOrderView_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
        {
            OnPop?.Invoke(sender, e);
        }

        private void LoadedCharacter_OnDelete(object? sender, ValueChangedArgs<MutableCharacter> e)
        {
            Delete();
        }

        /// <summary>
        /// Disable editting of characters that are from a character repository
        /// </summary>
        public void Lock()
        {
            IsAbstractNightEnabledCheckBox.IsEnabled = false;
            AbstractNightReminderTextBox.IsEnabled = false;
        }

        /// <summary>
        /// Enable editting of a character that are from a character repository
        /// </summary>
        public void Unlock()
        {
            IsAbstractNightEnabledCheckBox.IsEnabled = true;
            AbstractNightReminderTextBox.IsEnabled = IsAbstractNightEnabledCheckBox.IsChecked is true;
        }

        /// <inheritdoc />
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
                    int index = LoadedList.FindIndex(c => Array.IndexOf(NightIndexes, c.Id) >= Array.IndexOf(NightIndexes, LoadedCharacter.Id));
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
}
