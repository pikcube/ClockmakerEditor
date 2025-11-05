using System.ComponentModel;
using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs
{
    /// <summary>
    /// A complete implementation of AbstractNightOrder to show the night order on nights after the first
    /// </summary>
    public class OtherNightOrder : AbstractNightOrder
    {
        /// <summary>
        /// Load the data into night order
        /// </summary>
        /// <param name="loadedCharacter">The character to load</param>
        /// <param name="loadedScript">The script to load</param>
        /// <param name="loader">The image loader for the script</param>
        public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader)
        {
            base.Load(loadedCharacter, loadedScript, loader, loadedScript.Meta.OtherNight.Contains(loadedCharacter), loadedScript.Meta.OtherNight, loadedCharacter.OtherNightReminder);

            IsAbstractNightEnabledCheckBox.Content = "Other Night";
        }

        /// <inheritdoc />
        protected override string? GetReminder(MutableCharacter character) => character.OtherNightReminder;

        /// <inheritdoc />
        protected override void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AbstractNightReminderTextBox.Text = e.PropertyName switch
            {
                nameof(LoadedCharacter.OtherNightReminder) => LoadedCharacter.OtherNightReminder,
                _ => AbstractNightReminderTextBox.Text
            };
        }

        /// <inheritdoc />
        protected override void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            LoadedCharacter.OtherNightReminder = AbstractNightReminderTextBox.Text;
        }

        /// <inheritdoc />
        protected override string[] NightIndexes => ScriptParse.OtherNightOrderIds;
    }
}
