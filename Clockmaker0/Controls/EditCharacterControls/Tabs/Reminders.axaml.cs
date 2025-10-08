using System.Data;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Clockmaker0.Data;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class Reminders : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    public Reminders()
    {
        InitializeComponent();
        _newReminderButton.Click += NewReminderButton_Click;
    }

    public void Load(MutableCharacter loadedCharacter)
    {
        LoadedCharacter = loadedCharacter;
        ReminderStack.Children.Clear();
        ReminderStack.Children.Add(_newReminderButton);
        ReminderStack.Children.AddRange(loadedCharacter.Reminders.Where(z => z.Parent == loadedCharacter).Select(t =>
        {
            EditReminderToken e = new();
            e.Load(t, ReminderStack.Children.Remove);
            return e;
        }) ?? throw new NoNullAllowedException());

        LoadedCharacter.Reminders.ItemAdded += ReminderTokens_ItemAdded;
    }

    private void ReminderTokens_ItemAdded(object? sender, ValueChangedArgs<ReminderToken> e)
    {
        if (e.NewValue.Parent != LoadedCharacter)
        {
            return;
        }

        EditReminderToken token = new();

        token.Load(e.NewValue, ReminderStack.Children.Remove);
        if (e.NewValue is NewReminderToken nrt && nrt.OriginHashCode == GetHashCode())
        {
            token.ReminderTextBox.BufferFocus();
        }

        ReminderStack.Children.Add(token);

    }

    private void NewReminderButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadedCharacter.Reminders.Add(new NewReminderToken
        {
            IsGlobal = false,
            Parent = LoadedCharacter ?? throw new NoNullAllowedException(),
            Text = "",
            OriginHashCode = GetHashCode()
        });
    }

    private readonly Button _newReminderButton = new()
    {
        Content = "Add Reminder Token",
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    public void Delete()
    {
        _newReminderButton.Click -= NewReminderButton_Click;
        foreach (EditReminderToken editReminderToken in ReminderStack.Children.OfType<EditReminderToken>().ToArray())
        {
            editReminderToken.Delete();
        }
    }

    public class NewReminderToken : ReminderToken
    {
        [JsonIgnore]
        public required int OriginHashCode { get; init; }
    }
}