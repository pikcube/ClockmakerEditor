using System.ComponentModel;
using Avalonia.Controls;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs
{
    public class FirstNightOrder : AbstractNightOrder
    {
        public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript,
            ScriptImageLoader loader)
        {
            base.Load(loadedCharacter, loadedScript, loader, loadedScript.Meta.FirstNight.Contains(loadedCharacter), loadedScript.Meta.FirstNight, loadedCharacter.FirstNightReminder);

            IsAbstractNightEnabledCheckBox.Content = "First Night";
        }


        protected override string? GetReminder(MutableCharacter character) => character.FirstNightReminder;

        protected override void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            AbstractNightReminderTextBox.Text = e.PropertyName switch
            {
                nameof(LoadedCharacter.FirstNightReminder) => LoadedCharacter.FirstNightReminder,
                _ => AbstractNightReminderTextBox.Text
            };
        }

        protected override double? GetNightIndex(MutableCharacter mutableCharacter) => mutableCharacter.FirstNight;

        protected override void SetNightIndex(MutableCharacter loadedCharacter, double? i)
        {
            loadedCharacter.FirstNight = i;
        }

        protected override void AbstractNightReminderTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            LoadedCharacter.FirstNightReminder = AbstractNightReminderTextBox.Text;
        }
    }
}
