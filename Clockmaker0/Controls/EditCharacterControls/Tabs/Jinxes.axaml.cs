using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

/// <summary>
/// Control for creating and editting jinxes
/// </summary>
public partial class Jinxes : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    /// <inheritdoc />
    public Jinxes()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the current character and script data. May only be called once.
    /// </summary>
    /// <param name="loadedCharacter">The character to load</param>
    /// <param name="loadedScript">The script containing the character</param>
    public void Load(MutableCharacter loadedCharacter, MutableBotcScript loadedScript)
    {
        LoadedCharacter = loadedCharacter;
        LoadedScript = loadedScript;

        JinxesStack.Children.AddRange(loadedScript.Jinxes.Where(j => j.Parent == loadedCharacter.Id).Select(NewEditJinx));


        loadedScript.Jinxes.ItemAdded += Jinxes_ItemAdded;
    }

    private EditJinx NewEditJinx(MutableJinx j)
    {
        EditJinx ej = new();
        ej.Load(j, LoadedScript);
        ej.OnDelete += Ej_OnDelete;
        if (j is NewMutableJinx nmj && nmj.OriginHashCode == GetHashCode())
        {
            ej.JinxTextBox.BufferFocus();
        }
        return ej;
    }

    private void Ej_OnDelete(object? sender, SimpleEventArgs<EditJinx> e)
    {
        JinxesStack.Children.Remove(e.Value);
    }

    private void Jinxes_ItemAdded(object? sender, ValueChangedArgs<MutableJinx> e)
    {
        if (e.NewValue.Parent == LoadedCharacter.Id)
        {
            JinxesStack.Children.Add(NewEditJinx(e.NewValue));
        }
    }

    /// <inheritdoc />
    public void Delete()
    {
        JinxesStack.Children.Clear();
    }

    private void CreateJinxButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadedScript.Jinxes.Add(new NewMutableJinx("", LoadedCharacter.Id, LoadedScript.Characters.FirstOrDefault()?.Id ?? "", GetHashCode()));
    }

    /// <inheritdoc />
    public class NewMutableJinx(string reason, string parent, string child, int originHashCode) : MutableJinx(reason, parent, child)
    {
        /// <summary>
        /// The hash code of the object that created the jinx
        /// </summary>
        public int OriginHashCode { get; init; } = originHashCode;
    }
}