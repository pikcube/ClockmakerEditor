using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Clockmaker0.Data;
using Clockmaker0.Data.Medo;
using ImageMagick;
using Pikcube.AddIns;
using Pikcube.ReadWriteScript.Core.Mutable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Clockmaker0;

public class App : Application
{
    public static App? Instance { get; set; }
    public static IClassicDesktopStyleApplicationLifetime? Desktop { get; private set; }
    public static Window MainWindow => Desktop?.MainWindow ?? throw new NoNullAllowedException();
    private static ConcurrentDictionary<Key, bool> KeyState { get; } = new(Enum.GetValues<Key>().Distinct().Select(k => new KeyValuePair<Key, bool>(k, false)));
    public static IEnumerable<MainWindow> Windows => OpenWindows.Keys.OfType<MainWindow>();

    public static string BetaVersionNumber => "0.1.0.4";

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

    public static event EventHandler<KeyEventArgs>? OnKeyChanged;

    public class KeyEventArgs(Key k, bool isDown) : EventArgs
    {
        public Key KeyPressed { get; init; } = k;
        public bool IsDown { get; init; } = isDown;
    }

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

    public static bool IsWindowOpen(MainWindow mainWindow) => OpenWindows.ContainsKey(mainWindow);

    public static bool IsOnlyWindowOpened(MainWindow mainWindow) => IsWindowOpen(mainWindow) && OpenWindows.Count == 1;

    private static ConcurrentDictionary<Window, bool> OpenWindows { get; } = [];

    public static void AddOpenWindow(Window window)
    {
        OpenWindows.GetOrAdd(window, false);
    }

    public static void CloseWindow(Window window)
    {
        OpenWindows.Remove(window, out _);
        if (OpenWindows.IsEmpty)
        {
            Desktop?.Shutdown();
            return;
        }
        if (Desktop?.MainWindow == window)
        {
            Desktop.MainWindow = OpenWindows.First().Key;
        }

    }

    public static void CloseAllWindows()
    {
        Desktop?.Shutdown();
    }

    public static void SaveAll(object? sender, RoutedEventArgs routedEventArgs)
    {
        foreach (MainWindow w in OpenWindows.Keys.OfType<MainWindow>())
        {
            w.SaveMenuItem_OnClick(sender, routedEventArgs);
        }
    }

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
            (await Clockmaker0.MainWindow.CreateAsync(e.Args)).Show();
        });
    }

    private Queue<string[]> Args { get; init; } = [];

    public override void OnFrameworkInitializationCompleted()
    {
        if (Instance is not null)
        {
            throw new HighlanderException(nameof(Instance));
        }

        TokenStore.RotateSecrets();

        Instance = this;

        if (Desktop is not null)
        {
            throw new HighlanderException(nameof(Desktop));
        }


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            TaskManager.ScheduleTask(async () =>
            {
                if (desktop.Args is null)
                {
                    desktop.MainWindow = Clockmaker0.MainWindow.Create();
                }
                else
                {
                    desktop.MainWindow = await Clockmaker0.MainWindow.CreateAsync(desktop.Args);
                }

                desktop.MainWindow.Show();
                while (Args.Count > 0)
                {
                    (await Clockmaker0.MainWindow.CreateAsync(Args.Dequeue())).Show();
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static async Task CopyCharacterAsync(MutableCharacter originalCharacter, ScriptImageLoader originalLoader, MutableBotcScript targetScript, MutableCharacter loadedCharacter, ScriptImageLoader targetLoader, TrackedList<MutableJinx> allJinxes, Action<MutableCharacter> addFunction)
    {
        MutableCharacter copyCharacter = originalCharacter.MakeCopy();
        addFunction(copyCharacter);
        for (int n = 0; n < originalCharacter.Image.Count; ++n)
        {
            Bitmap img = await originalLoader.GetImageAsync(originalCharacter, n);
            await targetLoader.TrySetImageAsync(copyCharacter, n, s =>
            {
                img.Save(s);
                return Task.CompletedTask;
            }, MagickFormat.Png);
        }

        int targetIndex = targetScript.Characters.IndexOf(copyCharacter);

        targetScript.Characters.MoveIndexOfToIndexOf(targetIndex, targetScript.Characters.Count - 1);

        targetIndex = targetScript.Characters.IndexOf(copyCharacter);
        int positionIndex = targetScript.Characters.IndexOf(loadedCharacter);

        targetScript.Characters.MoveIndexOfToIndexOf(targetIndex, positionIndex);
        targetScript.Jinxes.AddRange(allJinxes.Where(j => j.Parent == originalCharacter.Id).Select(j => new MutableJinx(j.Rule, copyCharacter.Id, j.Child)));
    }
}