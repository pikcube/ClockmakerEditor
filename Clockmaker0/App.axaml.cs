using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Clockmaker0.Data;
using Clockmaker0.Data.Medo;
using ImageMagick;
using Pikcube.AddIns;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clockmaker0;

/// <inheritdoc />
public class App : Application
{
    /// <summary>
    /// Contains all properties that are saved between app sessions
    /// </summary>
    public static PersistentDataStore DataStore { get; private set; } = new();
    private static App? Instance { get; set; }
    private static IClassicDesktopStyleApplicationLifetime? Desktop { get; set; }
    private static ConcurrentDictionary<Key, bool> KeyState { get; } = new(Enum.GetValues<Key>().Distinct().Select(k => new KeyValuePair<Key, bool>(k, false)));
    /// <summary>
    /// Contains every currently open Window
    /// </summary>
    public static IEnumerable<MainWindow> Windows => OpenWindows.Keys.OfType<MainWindow>();

    /// <summary>
    /// Represents the Current App Version
    /// </summary>
    public static string BetaVersionNumber => "0.1.0.7";

    /// <summary>
    /// Set that a particular key is up or down
    /// </summary>
    /// <param name="k">Key name</param>
    /// <param name="b">True when key is pressed, false when key is released</param>
    public static void SetKeyState(Key k, bool b)
    {
        lock (KeyState)
        {
            if (KeyState[k] == b)
            {
                return;
            }

            KeyState[k] = b;
        }
        OnKeyChanged?.Invoke(k, new KeyEventArgs(k, KeyState[k]));
    }

    /// <summary>
    /// Raised when a key is pressed or released
    /// </summary>
    public static event EventHandler<KeyEventArgs>? OnKeyChanged;

    /// <summary>
    /// Encapsulates which key was changed and its current state
    /// </summary>
    /// <param name="k">Key name</param>
    /// <param name="isDown">True when key is pressed, false when key is released</param>
    public class KeyEventArgs(Key k, bool isDown) : EventArgs
    {
        /// <summary>
        /// The key that was pressed
        /// </summary>
        public Key KeyPressed { get; init; } = k;
        /// <summary>
        /// True when key is pressed, false when key is released
        /// </summary>
        public bool IsDown { get; init; } = isDown;
    }

    /// <summary>
    /// Function that checks if any of the provided keys are down
    /// </summary>
    /// <param name="keys">Keys to check</param>
    /// <returns>True when any of the keys are down</returns>
    public static bool IsKeyDown(params Span<Key> keys)
    {
        foreach (Key key in keys)
        {
            if (KeyState[key])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if this window is open or now
    /// </summary>
    /// <param name="mainWindow">The window to check</param>
    /// <returns>True if the window is open, false otherwise</returns>
    public static bool IsWindowOpen(MainWindow mainWindow) => OpenWindows.ContainsKey(mainWindow);

    /// <summary>
    /// Checks if this window is the only Window Open
    /// </summary>
    /// <param name="mainWindow">The window to check</param>
    /// <returns>True if the window is open, false otherwise</returns>
    public static bool IsOnlyWindowOpened(MainWindow mainWindow) => IsWindowOpen(mainWindow) && OpenWindows.Count == 1;

    private static ConcurrentDictionary<Window, bool> OpenWindows { get; } = [];

    /// <summary>
    /// Marks the window as open. Called by the WindowOpened event in Main Window
    /// </summary>
    /// <param name="window"></param>
    public static void AddOpenWindow(Window window)
    {
        OpenWindows.GetOrAdd(window, false);
        if (Desktop is null)
        {
            return;
        }

        Desktop.MainWindow ??= window;
    }

    /// <summary>
    /// Marks the window as closed. If no open windows remain after this, the program shuts down.
    /// </summary>
    /// <param name="window"></param>
    public static void CloseWindow(Window window)
    {
        OpenWindows.Remove(window, out _);
        if (OpenWindows.IsEmpty)
        {
            PersistentDataStore.Save(DataStore);
            Desktop?.Shutdown();
            return;
        }
        if (Desktop?.MainWindow == window)
        {
            Desktop.MainWindow = OpenWindows.First().Key;
        }

    }

    /// <summary>
    /// Call each window's Close Method then shut down
    /// </summary>
    public static void CloseAllWindows()
    {
        foreach (Window window in OpenWindows.Keys)
        {
            window.Close();
        }
        PersistentDataStore.Save(DataStore);
        Desktop?.Shutdown();
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        HighlanderValidator.Challenge();
        HighlanderValidator.ChallengerDetected += OnChallengerDetected;

        AvaloniaXamlLoader.Load(this);
        MagickNET.Initialize();
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Default;
    }

    private void OnChallengerDetected(object? sender, NewInstanceEventArgs e)
    {
        if (Desktop is null)
        {
            Args.Enqueue(e.Args);
            return;
        }

        TaskManager.ScheduleAsyncTask(async () =>
        {
            await MainWindow.OpenScriptsAsync(e.Args);
        });
    }

    private Queue<string[]> Args { get; init; } = [];

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (Instance is not null)
        {
            throw new HighlanderException(nameof(Instance));
        }
        DataStore = PersistentDataStore.Load();
        TokenStore.RotateSecrets();

        RequestedThemeVariant = DataStore.Theme switch
        {
            ThemeEnum.Auto => ThemeVariant.Default,
            ThemeEnum.Dark => ThemeVariant.Dark,
            ThemeEnum.Light => ThemeVariant.Light,
            _ => throw new ArgumentOutOfRangeException()
        };

        DataStore.PropertyChanged += DataStore_PropertyChanged;

        Instance = this;

        if (Desktop is not null)
        {
            throw new HighlanderException(nameof(Desktop));
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Desktop.ShutdownRequested += Desktop_ShutdownRequested;
            Desktop.Exit += Desktop_Exit;


            TaskManager.ScheduleTask(async () =>
            {
                if (desktop.Args is null)
                {
                    MainWindow.Create().Show();
                }
                else
                {
                    await MainWindow.OpenScriptsAsync(desktop.Args);
                }
                while (Args.Count > 0)
                {
                    await MainWindow.OpenScriptsAsync(Args.Dequeue());
                }

                if (OpenWindows.IsEmpty)
                {
                    MainWindow.Create().Show();
                }

                if (OpenWindows.IsEmpty)
                {
                    //Just in case I somehow completely screw this up
                    throw new Exception("Fatal error: Could not open window despite opening window");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DataStore_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DataStore.Theme):
                RequestedThemeVariant = DataStore.Theme switch
                {
                    ThemeEnum.Auto => ThemeVariant.Default,
                    ThemeEnum.Dark => ThemeVariant.Dark,
                    ThemeEnum.Light => ThemeVariant.Light,
                    _ => RequestedThemeVariant
                };
                return;
        }
    }

    private static void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        PersistentDataStore.Save(DataStore);
    }

    private static void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        PersistentDataStore.Save(DataStore);
    }

    /// <summary>
    /// Get the oldest existing MainWindow object if it exists
    /// </summary>
    public static MainWindow? OldestMainWindow => Desktop?.MainWindow as MainWindow;
}