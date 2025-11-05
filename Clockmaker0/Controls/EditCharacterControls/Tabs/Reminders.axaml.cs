using System.Data;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Clockmaker0.Data;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

/// <summary>
/// Control for adding and removing reminders
/// </summary>
public partial class Reminders : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;

    /// <inheritdoc />
    public Reminders()
    {
        InitializeComponent();
        _newReminderButton.Click += NewReminderButton_Click;
    }

    /// <summary>
    /// Load the reminders into the control
    /// </summary>
    /// <param name="loadedCharacter">The character to load in</param>
    public void Load(MutableCharacter loadedCharacter)
    {
        LoadedCharacter = loadedCharacter;
        ReminderStack.Children.Clear();
        ReminderStack.Children.Add(_newReminderButton);
        ReminderStack.Children.AddRange(loadedCharacter.Reminders.Where(z => z.Parent == loadedCharacter).Select(t =>
        {
            EditReminderToken e = new();
            e.Load(t);
            e.OnDelete += E_OnDelete;
            return e;
        }));

        LoadedCharacter.Reminders.ItemAdded += ReminderTokens_ItemAdded;
    }

    private void E_OnDelete(object? sender, EditReminderToken e)
    {
        ReminderStack.Children.Remove(e);
    }

    private void ReminderTokens_ItemAdded(object? sender, ValueChangedArgs<ReminderToken> e)
    {
        if (e.NewValue.Parent != LoadedCharacter)
        {
            return;
        }

        EditReminderToken token = new();

        token.Load(e.NewValue);
        token.OnDelete += E_OnDelete;
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

    /// <inheritdoc />
    public void Delete()
    {
        _newReminderButton.Click -= NewReminderButton_Click;
        foreach (EditReminderToken editReminderToken in ReminderStack.Children.OfType<EditReminderToken>().ToArray())
        {
            editReminderToken.Delete();
        }
    }

    /// <inheritdoc />
    public class NewReminderToken : ReminderToken
    {
        /// <summary>
        /// The hash code of the object that created the reminder. Used to determine the source of the reminder token
        /// </summary>
        [JsonIgnore]
        public required int OriginHashCode { get; init; }
    }
}