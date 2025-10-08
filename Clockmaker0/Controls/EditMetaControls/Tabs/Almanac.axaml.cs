using System.ComponentModel;
using Avalonia.Controls;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditMetaControls.Tabs;

public partial class Almanac : UserControl
{
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;

    public Almanac()
    {
        InitializeComponent();
    }

    public void Load(MutableMeta loadedMeta)
    {
        LoadedMeta = loadedMeta;
        AlmanacTextBox.Text = LoadedMeta.AlmanacData;

        AlmanacTextBox.TextChanged += AlmanacTextBox_TextChanged;
        LoadedMeta.PropertyChanged += LoadedMeta_PropertyChanged;
    }

    private void LoadedMeta_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        AlmanacTextBox.Text = e.PropertyName switch
        {
            nameof(LoadedMeta.AlmanacData) => LoadedMeta.AlmanacData,
            _ => AlmanacTextBox.Text
        };
    }

    private void AlmanacTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedMeta.AlmanacData = AlmanacTextBox.Text;
    }
}