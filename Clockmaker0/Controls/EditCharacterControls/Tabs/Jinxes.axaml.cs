using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class Jinxes : UserControl, IDelete
{
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    public Jinxes()
    {
        InitializeComponent();
    }

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
        ej.Load(j, JinxesStack.Children.Remove, LoadedScript);
        if (j is NewMutableJinx nmj && nmj.OriginHashCode == GetHashCode())
        {
            ej.JinxTextBox.BufferFocus();
        }
        return ej;
    }

    private void Jinxes_ItemAdded(object? sender, ValueChangedArgs<MutableJinx> e)
    {
        if (e.NewValue.Parent == LoadedCharacter.Id)
        {
            JinxesStack.Children.Add(NewEditJinx(e.NewValue));
        }
    }

    public void Delete()
    {
        JinxesStack.Children.Clear();
    }

    private void CreateJinxButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadedScript.Jinxes.Add(new NewMutableJinx("", LoadedCharacter.Id, LoadedScript.Characters.FirstOrDefault()?.Id ?? "", GetHashCode()));
    }

    public class NewMutableJinx(string reason, string parent, string child, int originHashCode) : MutableJinx(reason, parent, child)
    {
        public int OriginHashCode { get; init; } = originHashCode;
    }
}