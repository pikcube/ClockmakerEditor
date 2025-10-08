using System.ComponentModel;
using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs
{
    public class OtherNightOrder : AbstractNightOrder
    {
        public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader)
        {
            base.Load(loadedCharacter, loadedScript, loader, loadedScript.Meta.OtherNight.Contains(loadedCharacter), loadedScript.Meta.OtherNight, loadedCharacter.OtherNightReminder);

            IsAbstractNightEnabledCheckBox.Content = "Other Night";
        }

        protected override string? GetReminder(MutableCharacter character) => character.OtherNightReminder;

        protected override void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AbstractNightReminderTextBox.Text = e.PropertyName switch
            {
                nameof(LoadedCharacter.OtherNightReminder) => LoadedCharacter.OtherNightReminder,
                _ => AbstractNightReminderTextBox.Text
            };
        }

        protected override double? GetNightIndex(MutableCharacter mutableCharacter) => mutableCharacter.OtherNight;

        protected override void SetNightIndex(MutableCharacter loadedCharacter, double? i)
        {
            loadedCharacter.OtherNight = i;
        }

        protected override void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            LoadedCharacter.OtherNightReminder = AbstractNightReminderTextBox.Text;
        }
    }
}
