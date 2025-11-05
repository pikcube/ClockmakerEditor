using System.ComponentModel;
using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs
{
    /// <summary>
    /// A complete implementation of AbstractNightOrder to show the night order on night 1
    /// </summary>
    public class FirstNightOrder : AbstractNightOrder
    {
        /// <summary>
        /// Load the character and script into the first night order control
        /// </summary>
        /// <param name="loadedCharacter">The character to load</param>
        /// <param name="loadedScript">The script to load</param>
        /// <param name="loader">The image loader</param>
        public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader)
        {
            base.Load(loadedCharacter, loadedScript, loader, loadedScript.Meta.FirstNight.Contains(loadedCharacter), loadedScript.Meta.FirstNight, loadedCharacter.FirstNightReminder);

            IsAbstractNightEnabledCheckBox.Content = "First Night";
        }


        /// <inheritdoc />
        protected override string? GetReminder(MutableCharacter character) => character.FirstNightReminder;

        /// <inheritdoc />
        protected override void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AbstractNightReminderTextBox.Text = e.PropertyName switch
            {
                nameof(LoadedCharacter.FirstNightReminder) => LoadedCharacter.FirstNightReminder,
                _ => AbstractNightReminderTextBox.Text
            };
        }

        /// <inheritdoc />
        protected override void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            LoadedCharacter.FirstNightReminder = AbstractNightReminderTextBox.Text;
        }

        /// <inheritdoc />
        protected override string[] NightIndexes => ScriptParse.FirstNightOrderIds;
    }
}
