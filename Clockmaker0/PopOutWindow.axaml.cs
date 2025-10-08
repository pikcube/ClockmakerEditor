using System;
using Avalonia.Controls;
using Clockmaker0.Data;

namespace Clockmaker0;

public partial class PopOutWindow : Window
{

    public UserControl LoadedControl { get; set; }
    public PopOutWindow() : this(new UserControl(), "Pop Out Window")
    {
    }

    public PopOutWindow(UserControl control, string name)
    {
        InitializeComponent();
        ScrollViewer.Content = control;
        LoadedControl = control;
        Title = name;
        Closed += PopOutWindow_Closed;
    }


    private void PopOutWindow_Closed(object? sender, EventArgs e)
    {
        if (LoadedControl is IDelete deleteable)
        {
            deleteable.Delete();
        }
    }
}