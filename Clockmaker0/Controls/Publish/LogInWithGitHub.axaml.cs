using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Clockmaker0.Data;

namespace Clockmaker0.Controls.Publish;

/// <inheritdoc />
public partial class LogInWithGitHub : UserControl
{
    private ILauncher? Launcher { get; set; }
    private string Url { get; set; } = "";

    /// <inheritdoc />
    public LogInWithGitHub()
    {
        InitializeComponent();
        Loaded += LogInWithGitHub_Loaded;
    }

    private void LogInWithGitHub_Loaded(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            } while (Launcher is null);

            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Url);
            await Launcher.LaunchUriAsync(new Uri(Url));
        });
    }

    /// <summary>
    /// Load the current GitHub Login Screen
    /// </summary>
    /// <param name="code">The code to enter</param>
    /// <param name="url">The url to launch</param>
    /// <param name="launcher">The Launcher from Top Level</param>
    public void Load(string code, string url, ILauncher launcher)
    {
        Launcher = launcher;
        Url = url;
        InstructionTextBlock.Text = $"Open {url} in your browser and enter the following code";
        CodeTextBox.Text = code;
    }

}