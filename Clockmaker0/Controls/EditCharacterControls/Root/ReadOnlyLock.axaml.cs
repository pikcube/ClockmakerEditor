using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Clockmaker0.Data;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Root;

public partial class ReadOnlyLock : UserControl
{

    private string Id { get; set; } = "";
    private Func<Task>? Fork { get; set; }
    private ScriptImageLoader ImageLoader { get; set; } = ScriptImageLoader.Default;
    public ReadOnlyLock()
    {
        InitializeComponent();
    }

    public void Load(Func<Task> fork, string id, ScriptImageLoader loader)
    {
        Fork = fork;
        Id = id;
        ImageLoader = loader;
        ImageLoader.BeforeFork += ReadOnlyLock_BeforeFork;
    }

    private void ReadOnlyLock_BeforeFork(object? sender, ValueChangedArgs<string> e)
    {
        if (e.NewValue == Id)
        {
            Delete();
        }
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Fork?.Invoke();
    }

    public void Delete()
    {
        IsEnabled = false;
        IsVisible = false;
        ImageLoader.BeforeFork -= ReadOnlyLock_BeforeFork;
    }
}