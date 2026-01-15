using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Clockmaker0.Controls.CharacterImport;
using Clockmaker0.Controls.CharacterPreview;
using Clockmaker0.Controls.EditCharacterControls;
using Clockmaker0.Controls.Publish;
using Clockmaker0.Data;
using ImageMagick;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Clockmaker0.Printing;

namespace Clockmaker0;

/// <summary>
/// Control which loads in a script for editing and handles the logic of modifying the underlying Clockmaker File. (I really should have put more of this into its own class but too late now)
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    private const string MetaReadme = "This directory contains all the script art\r\n\r\nPath: script/{property}.png";
    private const string TokenReadme = "This directory contains all the token art for each character on the script\r\n\r\nPath: token/{id}/{index}.png";

    private int ImageVersion { get; set; } = -1;
    private Task<IStorageFolder?> DefaultFolder { get; set; }
    private ScriptImageLoader? ImageLoader { get; set; }
    private List<PopOutWindow> PopOutWindows { get; } = [];
    private List<IStorageFile> RecentlyOpenedFiles { get; } = [];

    private ZipArchive ClockFile
    {
        get;
        set
        {
            field.Dispose();
            field = value;
        }
    } = new(new MemoryStream(), ZipArchiveMode.Update);

    private IStorageFile? PathToOpenFile
    {
        get;
        set
        {
            field = value;
            if (field is not null)
            {
                DefaultFolder = field.GetParentAsync();
            }
        }
    }

    private MutableBotcScript? LoadedScript
    {
        get;
        set
        {
            if (field != null)
            {
                field.Characters.OrderChanged -= Characters_OrderChanged;
            }

            field = value;

            if (field != null)
            {
                field.Characters.OrderChanged += Characters_OrderChanged;
            }
        }
    }


    /// <inheritdoc />
    [UsedImplicitly]
    public MainWindow()
    {
        InitializeComponent();
        DefaultFolder = StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);

        CharacterSheetPanel.AddHandler(DragDrop.DragOverEvent, DragOver);
        CharacterSheetPanel.AddHandler(DragDrop.DropEvent, Drop);
        ExistingCharacterMenuItem.Items.Clear();
        ExistingCharacterMenuItem.ItemsSource = (MenuItem[])[OfficialCharacterMenuItem, FromScriptMenuItem];

        Title = $"Clockmaker Beta {App.BetaVersionNumber}";
    }

    /// <summary>
    /// Create a MainWindow object
    /// </summary>
    /// <param name="defaultFolder">Optional, a task which returns a default folder for opening and closing files</param>
    /// <returns>The MainWindow control</returns>
    public static MainWindow Create(Task<IStorageFolder?>? defaultFolder = null)
    {
        MainWindow mw = new();
        mw.DefaultFolder = defaultFolder ?? mw.DefaultFolder;
        mw.NewScript();
        return mw;
    }

    /// <summary>
    /// Create a MainWindow control and async load each of the provided scripts. If multiple scripts are provided, they will open in separate windows
    /// </summary>
    /// <param name="paths">The scripts top open</param>
    /// <returns>The window (unloaded)</returns>
    [UsedImplicitly]
    public static async Task<MainWindow> CreateAndShowAsync(params IStorageFile[] paths)
    {
        if (paths.Length == 0)
        {
            return Create();
        }

        MainWindow mw = new();
        await mw.LoadFilesAsync(paths);
        return mw;
    }


    //Internal because external libraries should prefer CreateAsync so the Storage Provider can safely validate the args for them
    internal static async Task OpenScriptsAsync(string[] desktopArgs)
    {
        if (desktopArgs.Length == 0)
        {
            Create().Show();
            return;
        }

        MainWindow mw = App.OldestMainWindow ?? new MainWindow();

        IStorageProvider provider = mw.StorageProvider;

        foreach (string path in desktopArgs)
        {
            IStorageFile? file = await provider.TryGetFileFromPathAsync(path);
            if (file is null)
            {
                //Flag Check
                switch (path.ToLower())
                {
                    //Help Flag
                    case "-h":
                    case "--help":
                    case "/?":
                        Console.WriteLine("Usage: clockmaker path/file.clockmaker");
                        continue;
                    //Bad script, please ignore
                    case "--puppets":
                        await CreateAndShowPuppetsAsync();
                        continue;
                    //She skewers you
                    case "--wife":
                        CreateAndShowWife();
                        continue;
                    //Find someone who looks at you the way...
                    case "--patters":
                        CreateWithGoonAndShow();
                        continue;
                    //File Not Found
                    default:
                        //todo: Something
                        continue;
                }
            }

            await mw.LoadFileAndShowAsync(file);
        }

    }

    private static void CreateWithGoonAndShow()
    {
        MainWindow w = Create();
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["goon"]);
        w.Show();
    }

    private static void CreateAndShowWife()
    {
        MainWindow w = Create();
        MutableBotcScript script = w.LoadedScript ?? throw new NoNullAllowedException();
        MutableMeta meta = script.Meta;
        meta.Name = "She Skewers You";
        meta.Author = "The Razor";
        w.AddCharacterToScript(CreateRazor(1, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(2, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(3, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(4, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(5, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(6, TeamEnum.Townsfolk));
        w.AddCharacterToScript(CreateRazor(7, TeamEnum.Outsider));
        w.AddCharacterToScript(CreateRazor(8, TeamEnum.Outsider));
        w.AddCharacterToScript(CreateRazor(9, TeamEnum.Minion));
        w.AddCharacterToScript(CreateRazor(10, TeamEnum.Minion));
        w.AddCharacterToScript(CreateRazor(11, TeamEnum.Demon));
        w.AddCharacterToScript(CreateRazor(12, TeamEnum.Demon));
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["bootlegger"]);
        meta.Bootlegger.Add(new BootlegRule("She skewers you"));
        w.Show();
    }

    private static Character CreateRazor(int index, TeamEnum team) => new() { Id = $"razor{index}", Edition = "", Name = "The Razor", Ability = "She Skewers You", Team = team, };

    private static async Task CreateAndShowPuppetsAsync()
    {
        MainWindow w = Create();
        MutableBotcScript script = w.LoadedScript ?? throw new NoNullAllowedException();
        MutableMeta meta = script.Meta;
        meta.Name = "Puppets";
        meta.Author = "Pikcube";
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["steward"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["shugenja"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["snakecharmer"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["fisherman"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["soldier"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["banshee"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["lunatic"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["drunk"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["marionette"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["lordoftyphon"]);
        w.AddCharacterToScript(ScriptParse.GetOfficialCharacters["sentinel"]);
        ScriptImageLoader scriptImageLoader = w.ImageLoader ?? throw new NoNullAllowedException();
        await scriptImageLoader.ForkAsync(script.Characters.Single(c => c.Id == "marionette"), script, "twoMarionette");
        MutableCharacter marionette = script.Characters.Single(c => c.Id == "twoMarionette");
        marionette.MutableAppFeatures.Selection = SelectionRule.Duplicates;
        meta.Bootlegger.Add(new BootlegRule("All minions are Marionette"));
        meta.Bootlegger.Add(new BootlegRule("If the demon moves, so do the Marionettes"));
        meta.Bootlegger.Add(new BootlegRule("Before creating a bag, remove all characters, change scripts, and play something else"));
        w.Show();
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        DragOverPreview(null, null);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {

    }

    private void DragOverPreview(MutableCharacter? target, PreviewScriptCharacter? source)
    {
        UpdatePreviews(target, source);
        foreach (MainWindow window in App.Windows)
        {
            if (window == this)
            {
                continue;
            }
            window.UpdatePreviews(null, null);
        }
    }

    private void UpdatePreviews(MutableCharacter? target, PreviewScriptCharacter? source)
    {
        bool dropAfter = false;
        foreach (PreviewScriptCharacter psc in CharacterSheetPanel.Items.OfType<PreviewScriptCharacter>())
        {
            psc.ReactToDragOver(target, dropAfter);
            if (psc == source)
            {
                dropAfter = true;
            }
        }
    }

    private static bool IsZipFile(Stream s)
    {
        Span<byte> target = [0x50, 0x4b, 0x03, 0x04];
        for (int n = 0; n < 4; ++n)
        {
            int nextByte = s.ReadByte();
            if (nextByte != -1 && nextByte == target[n])
            {
                continue;
            }

            s.Position = 0;
            return false;
        }

        s.Position = 0;
        return true;
    }

    private static async Task DisplayErrors(ReadOnlyCollection<string> errors, MainWindow window)
    {
        if (errors.Count > 0)
        {
            await MessageBoxManager.GetMessageBoxStandard(title: "Errors on load", text: "The following errors were encountered when loading the script" + Environment.NewLine + string.Join(Environment.NewLine, errors),
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner).ShowAsPopupAsync(window);
        }
    }

    private async Task LoadFilesAsync(IStorageFile[] paths)
    {
        foreach (IStorageFile file in paths)
        {
            await LoadFileAndShowAsync(file);
        }
    }

    private async Task LoadFileAndShowAsync(IStorageFile file)
    {
        await using Stream s = await file.OpenReadAsync();

        if (!IsZipFile(s))
        {
            return;
        }

        if (ImageVersion == -1)
        {
            PathToOpenFile?.Dispose();
            PathToOpenFile = file;
            ClockFile.Dispose();

            await LoadScriptFileAsync(file);
            Show();
        }
        else
        {
            await CreateAndShowAsync(file);
        }
    }

    private static bool IsInterruptClose(MutableBotcScript? loadedScript, ScriptImageLoader? imageLoader, IStorageFile? pathToOpenFile, int imageVersion)
    {
        if (loadedScript is null || imageLoader is null)
        {
            return false;
        }
        bool scriptChanged = loadedScript.IsChanged();
        bool imageChanged = imageLoader.IsChanged(imageVersion);
        return loadedScript.Characters.Count > 0 && (pathToOpenFile is null || scriptChanged || imageChanged);
    }


    private async Task<bool> IsCancelCloseAsync()
    {
        if (!IsInterruptClose(LoadedScript, ImageLoader, PathToOpenFile, ImageVersion))
        {
            return false;
        }

        if (LoadedScript == null)
        {
            return false;
        }

        ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Save File Before Closing?",
            $"Would you like to save {LoadedScript.Meta.Name} before closing?",
            ButtonEnum.YesNoCancel).ShowWindowDialogAsync(this);

        return result switch
        {
            ButtonResult.Yes => !await TrySaveAsync(LoadedScript, ClockFile, StorageProvider, await DefaultFolder, PathToOpenFile),
            ButtonResult.No => false,
            _ => true
        };
    }

    /// <summary>
    /// Dispose of the MainWindow and underlying script file
    /// </summary>
    public void Dispose()
    {
        ClockFile.Dispose();
        PathToOpenFile?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void LoadEditControl(UserControl control)
    {
        EditPanel.Children.Clear();

        EditPanel.Children.Add(control);
    }

    private void PrepareToSave()
    {
        if (LoadedScript is null)
        {
            return;
        }
        LoadedScript.Save();
        ClockFile.GetEntry("script.json")?.Delete();
        ZipArchiveEntry entry = ClockFile.CreateEntry("script.json");
        using Stream stream = entry.Open();
        using StreamWriter writer = new(stream);
        writer.Write(ScriptParse.SerializeScript(LoadedScript, Formatting.Indented, isExtended: false));
    }

    private static async Task PrepareToSaveAsync(MutableBotcScript loadedScript, ZipArchive clockFile)
    {
        loadedScript.Save();
        clockFile.GetEntry("script.json")?.Delete();
        ZipArchiveEntry entry = clockFile.CreateEntry("script.json");
        await using Stream stream = entry.Open();
        await using StreamWriter writer = new(stream);
        await writer.WriteAsync(ScriptParse.SerializeScript(loadedScript, Formatting.Indented, isExtended: false));

        clockFile.GetEntry("almanac.md")?.Delete();
        ZipArchiveEntry almanacEntry = clockFile.CreateEntry("almanac.md");
        await using Stream almanacStream = almanacEntry.Open();
        await using StreamWriter almanacWriter = new(almanacStream);
        await writer.WriteAsync(loadedScript.Meta.AlmanacData ?? "");

    }

    private async Task LoadScriptFileAsync(IStorageFile script)
    {
        await using Stream s = await script.OpenReadAsync();
        string almanac = string.Empty;

        if (IsZipFile(s))
        {
            PathToOpenFile = script;
            (LoadedScript, ClockFile) = await LoadClockmakerFileAsync(script);
            ZipArchiveEntry? almanacEntry = ClockFile.GetEntry("almanac.md");
            if (almanacEntry is not null)
            {
                await using Stream almanacStream = almanacEntry.Open();
                using StreamReader reader = new(almanacStream);
                almanac = await reader.ReadToEndAsync();
            }
            App.DataStore.RecentlyOpenedFiles.Add(script.Path.LocalPath);
        }

        else
        {
            PathToOpenFile = null;
            (LoadedScript, ClockFile) = await LoadJsonFileAsync(script);
        }


        await ValidateClockmakerFileAsync(ClockFile, LoadedScript);

        InitializeScript(LoadedScript, almanac);
    }

    private static async Task<(MutableBotcScript, ZipArchive)> LoadJsonFileAsync(IStorageFile file)
    {
        using StreamReader reader = new(await file.OpenReadAsync());
        ZipArchive clockFile = new(new MemoryStream(), ZipArchiveMode.Update);
        MutableBotcScript loadedScript = ScriptParse.DeserializeMutableBotcScript(await reader.ReadToEndAsync());

        loadedScript.Save();
        await using Stream scriptEntry = clockFile.CreateEntry("script.json").Open();
        await using StreamWriter writer = new(scriptEntry);
        await writer.WriteAsync(ScriptParse.SerializeScript(loadedScript));
        return (loadedScript, clockFile);
    }

    private static async Task<(MutableBotcScript, ZipArchive)> LoadClockmakerFileAsync(IStorageFile script)
    {
        await using Stream data = await script.OpenReadAsync();
        MemoryStream ms = new();

        await data.CopyToAsync(ms);

        ms.Position = 0;
        ZipArchive clockFile = new(ms, ZipArchiveMode.Update);

        ZipArchiveEntry entry = clockFile.GetEntry("script.json") ?? throw new NoNullAllowedException();
        await using Stream file = entry.Open();
        using StreamReader reader = new(file);

        MutableBotcScript loadedScript = ScriptParse.DeserializeMutableBotcScript(await reader.ReadToEndAsync());
        return (loadedScript, clockFile);
    }

    private static async Task ValidateClockmakerFileAsync(ZipArchive clockFile, MutableBotcScript loadedScript)
    {
        await AddEntryIfMissingAsync(clockFile, "script.json", ScriptParse.SerializeScript(loadedScript, Formatting.Indented, isExtended: false));
        await AddEntryIfMissingAsync(clockFile, "token/readme.txt", TokenReadme);
        await AddEntryIfMissingAsync(clockFile, "script/readme.txt", MetaReadme);
        await AddEntryIfMissingAsync(clockFile, "almanac.md", string.Empty);

        string[] allPaths = [.. GetAllPaths(loadedScript)];
        foreach (ZipArchiveEntry entry in clockFile.Entries.ToArray())
        {
            if (allPaths.Contains(entry.FullName))
            {
                continue;
            }

            entry.Delete();
        }

        loadedScript.Characters.SortBy(c => c.Team).SaveToTrackedList();
        loadedScript.Save();
    }

    private static async Task AddEntryIfMissingAsync(ZipArchive clockFile, string path, string value)
    {
        if (clockFile.GetEntry(path) is not null)
        {
            return;
        }

        ZipArchiveEntry entry = clockFile.CreateEntry(path);
        await using Stream entryStream = entry.Open();
        await using StreamWriter writer = new(entryStream);
        await writer.WriteAsync(value);
    }

    private static IEnumerable<string> GetAllPaths(BotcScript script)
    {
        if (script.Meta.Logo is not null)
        {
            yield return script.Meta.Logo;
        }

        if (script.Meta.Background is not null)
        {
            yield return script.Meta.Background;
        }

        foreach (string path in script.Characters.Where(c => !ScriptParse.IsOfficial(c.Id)).SelectMany(c => c.Image))
        {
            yield return path;
        }

        yield return "script.json";
        yield return "token/readme.txt";
        yield return "script/readme.txt";
        yield return "almanac.md";
        yield return "clockmaker.version";
    }

    private void InitializeScript(MutableBotcScript loadedScript, string almanac)
    {
        if (LoadedScript is not null)
        {
            LoadedScript.Meta.PropertyChanged -= Meta_PropertyChanged;
        }
        LoadedScript = loadedScript;
        LoadedScript.Meta.AlmanacData = string.IsNullOrEmpty(almanac) ? null : almanac;
        LoadedScript.Meta.PropertyChanged += Meta_PropertyChanged;
        Meta_PropertyChanged(LoadedScript.Meta, new PropertyChangedEventArgs(nameof(LoadedScript.Meta.Name)));

        //Init image loader
        ImageLoader = new ScriptImageLoader(ClockFile);
        ImageVersion = ImageLoader.Version;

        //Cleanup last script loaded
        foreach (Control control in CharacterSheetPanel.Items.OfType<Control>())
        {
            control.IsEnabled = false;
        }
        EditPanel.Children.Clear();

        //Init character sheet
        ScriptTitle scriptTitle = new();
        scriptTitle.Load(LoadedScript, ImageLoader);
        scriptTitle.OnLoadEdit += ScriptTitle_OnLoadEdit;
        scriptTitle.OnPop += ScriptTitle_OnPop;
        scriptTitle.OnDelete += ScriptTitle_OnDelete;
        scriptTitle.OnAddCharacter += ScriptTitle_OnAddCharacter;
        PreviewScriptCharacter[] previews = [.. LoadedScript.Characters.Select(c => CreateNewPreviewScriptCharacter(c, ImageLoader, LoadedScript))];
        Control[] value = [
            scriptTitle,
            ..previews
        ];
        CharacterSheetPanel.ItemsSource = value;

        ToggleScriptMenuItems(true);

        //Set initial focus
        scriptTitle.TitleTextBox.BufferFocus();

        if (App.IsWindowOpen(this))
        {
            TaskManager.ScheduleTask(() => DisplayErrors(LoadedScript.Errors, this));
        }

        TaskManager.ScheduleAsyncTask(async () => await InitializeImages(LoadedScript, ImageLoader));
    }

    private static async Task InitializeImages(MutableBotcScript loadedScript, ScriptImageLoader loader)
    {
        foreach (MutableCharacter character in loadedScript.Characters)
        {
            for (int n = 0; n < character.Image.Count; ++n)
            {
                if (character.Image[n].StartsWith("http"))
                {
                    await loader.GetCacheImageAsync(character, n);
                }
            }
        }
    }

    private void NewScript()
    {
        ClockFile = new ZipArchive(new MemoryStream(), ZipArchiveMode.Update);
        PathToOpenFile?.Dispose();
        PathToOpenFile = null;
        InitializeScript(new MutableBotcScript(new BotcScript(new Meta { Name = "" }, []), ScriptParse.Base), "");
    }

    private async Task OpenAsync()
    {
        IStorageFile? file = await OpenClockmakerFileAsync();

        if (file is null)
        {
            return;
        }

        if (LoadedScript is null || LoadedScript.Characters.Count == 0)
        {
            PathToOpenFile?.Dispose();
            PathToOpenFile = file;
            ClockFile.Dispose();

            await LoadScriptFileAsync(file);
        }
        else
        {
            await CreateAndShowAsync(file);
        }
    }

    private async Task<IStorageFile?> OpenClockmakerFileAsync()
    {
        IReadOnlyList<IStorageFile> filesList = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Clockmaker")
                {
                    Patterns = ["*.clockmaker"],
                    AppleUniformTypeIdentifiers = ["pikcube.clockmaker"],
                    MimeTypes = ["application/clockmaker"]
                },
                new FilePickerFileType("Script Json")
                {
                    Patterns = ["*.json"],
                    AppleUniformTypeIdentifiers = ["public.json"],
                    MimeTypes = ["application/json"]
                }
            ],
            SuggestedStartLocation = await DefaultFolder,
            Title = "Open Script"
        });

        if (filesList.Count != 1)
        {
            return null;
        }

        IStorageFile file = filesList.Single();
        return file;
    }

    private async Task<bool> TrySaveAsync(MutableBotcScript loadedScript, ZipArchive clockFile, IStorageProvider storageProvider, IStorageFolder? defaultFolder, IStorageFile? file = null)
    {
        await PrepareToSaveAsync(loadedScript, clockFile);

        file ??= await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = ".clockmaker",
            FileTypeChoices =
            [
                new FilePickerFileType("Clockmaker")
                {
                    Patterns = ["*.clockmaker"],
                    AppleUniformTypeIdentifiers = ["pikcube.clockmaker"],
                    MimeTypes = ["application/clockmaker"]
                }
            ],
            ShowOverwritePrompt = true,
            SuggestedFileName = loadedScript.Meta.Name,
            Title = "Save Clockmaker File",
            SuggestedStartLocation = defaultFolder
        });

        if (file is null)
        {
            return false;
        }

        await using Stream fileStream = await file.OpenWriteAsync();
        using ZipArchive newArchive = new(fileStream, ZipArchiveMode.Create);
        foreach (ZipArchiveEntry entry in clockFile.Entries)
        {
            await using Stream stream = entry.Open();
            ZipArchiveEntry newEntry = newArchive.CreateEntry(entry.FullName, CompressionLevel.SmallestSize);
            await using Stream newStream = newEntry.Open();
            await stream.CopyToAsync(newStream);
        }

        if (PathToOpenFile != file)
        {
            PathToOpenFile?.Dispose();
            PathToOpenFile = file;
        }

        ImageVersion = ImageLoader?.Version ?? -1;
        App.DataStore.RecentlyOpenedFiles.Add(file.Path.LocalPath);

        return true;
    }

    private static int GetInsertIndex(MutableCharacter characterToInsert, MutableBotcScript loadedScript)
    {
        int startIndex = loadedScript.Characters.FindIndex(c => c.Team >= characterToInsert.Team);
        if (startIndex == -1)
        {
            return -1;
        }

        if (characterToInsert.Ability.Length == 0)
        {
            return loadedScript.Characters.FindIndex(startIndex, c => c.Team != characterToInsert.Team);
        }

        return loadedScript.Characters.FindIndex(startIndex, currentCharacter =>
        {
            // If next character isn't on the same team, insert target before next character
            if (currentCharacter.Team != characterToInsert.Team)
            {
                return true;
            }

            // If next character goes after target in sort order, insert target before next character
            if (currentCharacter.SortInfo > characterToInsert.SortInfo)
            {
                return true;
            }

            //If next character goes before target in sort order, insert target after next character
            if (currentCharacter.SortInfo < characterToInsert.SortInfo)
            {
                return false;
            }

            //If next character's ability is longer than target, insert target before next character
            return currentCharacter.Ability.Length > characterToInsert.Ability.Length;
        });

    }

    private void ToggleScriptMenuItems(bool isEnabled)
    {
        CloseScriptMenuItem.IsEnabled = isEnabled;
        SaveAllMenuItem.IsEnabled = isEnabled;
        SaveScriptMenuItem.IsEnabled = isEnabled;
        SaveScriptAsMenuItem.IsEnabled = isEnabled;
        NewCharacterMenuItem.IsEnabled = isEnabled;
        PublishMenuItem.IsEnabled = isEnabled;

        ExistingCharacterMenuItem.IsEnabled = isEnabled;
    }

    private void PopAction(INamedControl<Control> namedControl)
    {
        PopAction(namedControl.Control, new PopOutWindow(namedControl));
    }

    private void PopAction(Control control, string name)
    {
        PopAction(control, new PopOutWindow(control, name));
    }

    private void PopAction(Control control, PopOutWindow popOutWindow)
    {
        EditPanel.Children.Remove(control);

        popOutWindow.Width = App.DataStore.ColumnCount switch
        {
            0 when Width < 1200 => Width,
            0 when EditPanel.Children.Count > 0 => MainGridView.ColumnDefinitions[2].ActualWidth,
            0 => Width / 2,
            1 => Width,
            _ => MainGridView.ColumnDefinitions[2].ActualWidth
        };

        popOutWindow.Show();
        PopOutWindows.Add(popOutWindow);
        popOutWindow.Closed += PopOutWindow_Closed;
    }



    private void AddNewCharacter(TeamEnum team)
    {
        while (LoadedScript is null)
        {
            NewScript();
        }

        MutableCharacter character = new()
        {
            Id = $"{LoadedScript.Meta.Name}_{LoadedScript.Characters.Count}",
            Edition = LoadedScript.Meta.Name,
            Name = "",
            Ability = "",
            Team = team
        };
        while (LoadedScript.Characters.Any(z => z.Id == character.Id))
        {
            character.Id += "_new";
        }
        AddCharacterToScript(character).BufferFocus();
    }

    private PreviewScriptCharacter AddCharacterToScript(Character character)
    {
        while (LoadedScript is null)
        {
            NewScript();
        }

        PreviewScriptCharacter psc = AddCharacterToScript(character.AsMutable());
        LoadedScript.Jinxes.AddRange(character.Jinxes.Select(j => new MutableJinx(j.Reason, character.Id, j.Id)));
        return psc;
    }

    private PreviewScriptCharacter AddCharacterToScript(MutableCharacter character)
    {
        while (LoadedScript is null)
        {
            NewScript();
        }

        ImageLoader ??= new ScriptImageLoader(ClockFile);

        int insertIndex = GetInsertIndex(character, LoadedScript);
        if (insertIndex == -1)
        {
            insertIndex = LoadedScript.Characters.Count;
        }
        LoadedScript.Characters.Insert(insertIndex, character);
        PreviewScriptCharacter previewScriptCharacter = CreateNewPreviewScriptCharacter(character, ImageLoader, LoadedScript);
        List<UserControl> itemList = [.. CharacterSheetPanel.Items.OfType<UserControl>()];
        itemList.Insert(insertIndex + 1, previewScriptCharacter);

        if (ScriptParse.FirstNightOrderIds.Contains(character.Id))
        {
            MutableCharacter? target = LoadedScript.Meta.FirstNight.FirstOrDefault(mc => Array.IndexOf(ScriptParse.FirstNightOrderIds, mc.Id) >= Array.IndexOf(ScriptParse.FirstNightOrderIds, character.Id));
            if (target is null)
            {
                LoadedScript.Meta.FirstNight.Add(character);
            }
            else
            {
                int targetIndex = LoadedScript.Meta.FirstNight.IndexOf(target);
                LoadedScript.Meta.FirstNight.Insert(targetIndex, character);
            }
        }

        if (ScriptParse.OtherNightOrderIds.Contains(character.Id))
        {
            MutableCharacter? target = LoadedScript.Meta.OtherNight.FirstOrDefault(mc => Array.IndexOf(ScriptParse.OtherNightOrderIds, mc.Id) >= Array.IndexOf(ScriptParse.OtherNightOrderIds, character.Id));
            if (target is null)
            {
                LoadedScript.Meta.OtherNight.Add(character);
            }
            else
            {
                int targetIndex = LoadedScript.Meta.OtherNight.IndexOf(target);
                LoadedScript.Meta.OtherNight.Insert(targetIndex, character);
            }
        }

        CharacterSheetPanel.ItemsSource = itemList;
        return previewScriptCharacter;
    }


    private async Task AddCharacterFromOpenClockmakerScript(MainWindow newWindow)
    {
        if (newWindow.ImageLoader is null)
        {
            return;
        }

        if (newWindow.LoadedScript is null)
        {
            return;
        }

        if (LoadedScript is null)
        {
            return;
        }

        if (ImageLoader is null)
        {
            return;
        }

        BotcScript loadedScript = newWindow.LoadedScript;

        IEnumerable<Character> loadedScriptCharacters = loadedScript.Characters.Where(loadedCharacter => LoadedScript.Characters.All(c => c.Id != loadedCharacter.Id));

        OfficialCharacterImport oci = new()
        {
            Width = Width / 4,
            Height = Height / 2,
        };

        oci.Load(newWindow.ImageLoader, loadedScriptCharacters);
        await oci.ShowDialog(this);
        if (!oci.IsConfirmed)
        {
            return;
        }

        foreach (Character c in oci.ImportedCharacters.OfType<Character>())
        {
            if (LoadedScript.Characters.Any(lc => lc.Id == c.Id))
            {
                continue;
            }

            MutableCharacter mc = c.AsMutable();

            if (!ScriptParse.IsOfficial(c.Id))
            {
                for (int index = 0; index < c.Image.Length; index++)
                {
                    await ImageLoader.TrySetImageAsync(mc, index, async target =>
                    {
                        Bitmap img = await newWindow.ImageLoader.GetImageAsync(c, index);
                        img.Save(target);
                    }, MagickFormat.Png);
                }
            }

            _ = AddCharacterToScript(mc);
            LoadedScript.Jinxes.AddRange(c.Jinxes.Select(j => new MutableJinx(j.Reason, c.Id, j.Id)));
        }
    }

    private void RemoveCharacterFromScript(MutableCharacter character)
    {
        while (LoadedScript is null)
        {
            NewScript();
        }

        LoadedScript.Characters.Remove(character);
        LoadedScript.Meta.FirstNight.Remove(character);
        LoadedScript.Meta.OtherNight.Remove(character);
        foreach (string p in character.Image)
        {
            ClockFile.GetEntry(p)?.Delete();
        }

        EditPanel.Children.RemoveAll(EditPanel.Children.OfType<EditCharacter>().Where(ec => ec.LoadedCharacter == character));
        foreach (PopOutWindow pow in PopOutWindows)
        {
            if (pow.LoadedControl is not EditCharacter ec)
            {
                continue;
            }

            if (ec.LoadedCharacter == character)
            {
                pow.Close();
            }
        }

        if (CharacterSheetPanel.ItemsSource == null)
        {
            return;
        }

        List<Control> controls = [.. CharacterSheetPanel.ItemsSource.OfType<Control>()];
        foreach (PreviewScriptCharacter psc in CharacterSheetPanel.Items.Source.OfType<PreviewScriptCharacter>().Where(psc => psc.LoadedCharacter == character))
        {
            controls.Remove(psc);
        }

        CharacterSheetPanel.ItemsSource = controls;
    }

    private void CloseScript()
    {
        LoadedScript = null;
        ImageLoader?.Dispose();
        ImageLoader = null;
        PathToOpenFile = null;
        ImageVersion = -1;
        foreach (IDisposable child in CharacterSheetPanel.Items.OfType<IDisposable>())
        {
            child.Dispose();
        }
        foreach (IDisposable child in EditPanel.Children.OfType<IDisposable>())
        {
            child.Dispose();
        }
        foreach (PopOutWindow popOutWindow in PopOutWindows)
        {
            popOutWindow.Close();
        }
        CharacterSheetPanel.ItemsSource = null;
        EditPanel.Children.Clear();

        if (App.IsOnlyWindowOpened(this))
        {
            ToggleScriptMenuItems(false);
            return;
        }

        Close();
    }

    //Begin Event Hooks

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        KeyDown += MainWindow_KeyDown;
        KeyUp += MainWindow_KeyUp;
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        KeyDown -= MainWindow_KeyDown;
        KeyUp -= MainWindow_KeyUp;
    }

    private void MainWindow_OnOpened(object? sender, EventArgs e)
    {
        App.AddOpenWindow(this);

        TaskManager.ScheduleTask(() => DisplayErrors(LoadedScript?.Errors ?? [], this));
        TaskManager.ScheduleAsyncTask(async () =>
        {
            List<IStorageFile> files = [];
            foreach (string s in App.DataStore.RecentlyOpenedFiles.ToArray())
            {
                IStorageFile? sf = await StorageProvider.TryGetFileFromPathAsync(s);
                if (sf is null)
                {
                    App.DataStore.RecentlyOpenedFiles.Remove(s);
                    continue;
                }
                files.Add(sf);
            }

            RecentlyOpenedFiles.Clear();
            RecentlyOpenedFiles.AddRange(files);

            App.DataStore.PropertyChanged += DataStoreRecentlyOpenedFilesChanged;
        });


    }

    private void DataStoreRecentlyOpenedFilesChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(App.DataStore.RecentlyOpenedFiles):
                TaskManager.ScheduleAsyncTask(async () =>
                {
                    List<IStorageFile> files = [];
                    foreach (string s in App.DataStore.RecentlyOpenedFiles.ToArray())
                    {
                        IStorageFile? sf = await StorageProvider.TryGetFileFromPathAsync(s);
                        if (sf is null)
                        {
                            App.DataStore.RecentlyOpenedFiles.Remove(s);
                            continue;
                        }
                        files.Add(sf);
                    }

                    RecentlyOpenedFiles.Clear();
                    RecentlyOpenedFiles.AddRange(files);
                });
                return;
            case nameof(App.DataStore.ColumnCount):
                UpdateResponsiveLayouts();
                return;
        }
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        App.CloseWindow(this);
        App.DataStore.PropertyChanged -= DataStoreRecentlyOpenedFilesChanged;
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        TaskManager.ScheduleTask(async () =>
        {
            e.Cancel = true;

            if (await IsCancelCloseAsync())
            {
                return;
            }

            Closing -= Window_OnClosing;
            Close();
        });
    }

    private static void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        App.SetKeyState(e.Key, true);
    }

    private static void MainWindow_KeyUp(object? sender, KeyEventArgs e)
    {
        App.SetKeyState(e.Key, false);
    }

    private void NewScript_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedScript is null || LoadedScript.Characters.Count == 0)
        {
            NewScript();
        }
        else
        {
            Create(DefaultFolder).Show();
        }
    }
    private void OpenMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            await OpenAsync();
        });
    }

    private void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedScript is null)
        {
            return;
        }
        TaskManager.ScheduleAsyncTask(async () =>
        {
            await TrySaveAsync(LoadedScript, ClockFile, StorageProvider, await DefaultFolder, PathToOpenFile);
        });
    }

    private void SaveAsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedScript is null)
        {
            return;
        }
        TaskManager.ScheduleAsyncTask(async () =>
        {
            await TrySaveAsync(LoadedScript, ClockFile, StorageProvider, await DefaultFolder);
        });
    }

    private void SaveAllMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (MainWindow w in App.Windows)
        {
            w.SaveMenuItem_OnClick(sender, e);
        }
    }

    private void TownsfolkMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Townsfolk);
    }

    private void OutsiderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Outsider);
    }

    private void MinionMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Minion);
    }

    private void DemonMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Demon);
    }

    private void TravelerMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Traveller);
    }

    private void FabledMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Fabled);
    }
    private void LoricMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        AddNewCharacter(TeamEnum.Loric);
    }

    private void PopOutWindow_Closed(object? sender, EventArgs e)
    {

    }

    private void AddOfficialCharacterMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            while (LoadedScript is null)
            {
                NewScript();
            }

            ImageLoader ??= new ScriptImageLoader(ClockFile);

            OfficialCharacterImport oci = new()
            {
                Width = Width / 4,
                Height = Height / 2,
            };


            IOrderedEnumerable<Character> officialCharacters = ScriptParse.GetOfficialCharacters.Values
                .Where(oc => LoadedScript.Characters.All(c => c.Id != oc.Id))
                .OrderBy(z => z.Name);

            oci.Load(ImageLoader, officialCharacters);
            Task result = oci.ShowDialog(this);
            await result;
            if (!oci.IsConfirmed)
            {
                return;
            }

            foreach (Character c in oci.ImportedCharacters.OfType<Character>())
            {
                if (LoadedScript.Characters.Any(lc => lc.Id == c.Id))
                {
                    continue;
                }

                _ = AddCharacterToScript(c);
            }
        });
    }


    private void AddCharacterFromClockmakerFileMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            while (LoadedScript is null)
            {
                NewScript();
            }

            ImageLoader ??= new ScriptImageLoader(ClockFile);

            IStorageFile? file = await OpenClockmakerFileAsync();
            if (file is null)
            {
                return;
            }

            using MainWindow newWindow = new();
            await newWindow.LoadScriptFileAsync(file);
            await AddCharacterFromOpenClockmakerScript(newWindow);

            newWindow.CloseScript();
        });
    }

    private void CloseMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleTask(async () =>
        {
            if (await IsCancelCloseAsync())
            {
                return;
            }

            CloseScript();
        });
    }

    private void ExitMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ExitAppMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        App.CloseAllWindows();
    }

    private void PublichGitHubMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedScript is null)
        {
            return;
        }

        PrepareToSave();

        //Note that this control is excluded from the public repository
        PublishScript publish = new(LoadedScript, ClockFile, PathToOpenFile);
        publish.ShowDialog(this);
    }

    private void ManualPublishMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedScript is null)
        {
            return;
        }

        LoadedScript.Save();

        TaskManager.ScheduleTask(async () =>
        {
            await PrepareToSaveAsync(LoadedScript, ClockFile);
            IStorageFile? saveFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = "*.zip",
                FileTypeChoices = [new FilePickerFileType("Zip File")
                {
                    AppleUniformTypeIdentifiers = ["public.zip-archive"],
                    MimeTypes = ["application/zip"],
                    Patterns = ["*.zip"]
                }]
            });

            if (saveFile == null)
            {
                return;
            }

            await using (Stream saveStream = await saveFile.OpenWriteAsync())
            {
                await using ZipArchive newArchive = new(saveStream, ZipArchiveMode.Create);
                foreach (ZipArchiveEntry entry in ClockFile.Entries)
                {
                    ZipArchiveEntry newEntry = newArchive.CreateEntry(entry.FullName);
                    await using Stream newStream = await newEntry.OpenAsync();
                    await using Stream oldStream = await entry.OpenAsync();
                    await oldStream.CopyToAsync(newStream);
                }
            }

            await Launcher.LaunchFileAsync(saveFile);
        });
    }

    private void StandardOrderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadedScript?.Characters.SortBy(c => c.Team).ThenBy(c => c.SortInfo).ThenBy(c => c.Ability.Length).SaveToTrackedList();
    }

    private void AlphabeticalOrderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadedScript?.Characters.SortBy(c => c.Team).ThenBy(c => c.Name).SaveToTrackedList();
    }

    private void ExistingCharacterMenuItem_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        List<MenuItem> items = [OfficialCharacterMenuItem];

        foreach (MainWindow window in App.Windows)
        {
            if (window == this)
            {
                continue;
            }

            if (window.LoadedScript is null)
            {
                continue;
            }

            string newHeader = $"From {window.LoadedScript.Meta.Name}...";
            MenuItem newItem = new()
            {
                Header = newHeader
            };
            MainWindow mw = window;
            newItem.Click += (_, _) =>
            {
                TaskManager.ScheduleAsyncTask(async () =>
                {
                    await AddCharacterFromOpenClockmakerScript(mw);
                });
            };
            items.Add(newItem);
        }


        items.Add(FromScriptMenuItem);
        ExistingCharacterMenuItem.ItemsSource = items;
    }

    private void Characters_OrderChanged(object? sender, ValueChangedArgs<TrackedList<MutableCharacter>> e)
    {
        if (ImageLoader is null || LoadedScript is null)
        {
            return;
        }

        e.NewValue.OrderChanged -= Characters_OrderChanged;
        e.NewValue.SortBy(c => c.Team).SaveToTrackedList();
        e.NewValue.OrderChanged += Characters_OrderChanged;


        ScriptTitle title = CharacterSheetPanel.Items.OfType<ScriptTitle>().Single();
        PreviewScriptCharacter[] characters = [.. e.NewValue.Select(mc =>
            CharacterSheetPanel.Items.OfType<PreviewScriptCharacter>().SingleOrDefault(psc => psc.LoadedCharacter == mc) ??
            CreateNewPreviewScriptCharacter(mc, ImageLoader, LoadedScript))];

        UserControl[] items = [title, .. characters];

        CharacterSheetPanel.ItemsSource = items;
    }

    private PreviewScriptCharacter CreateNewPreviewScriptCharacter(MutableCharacter mc, ScriptImageLoader imageLoader, MutableBotcScript loadedScript)
    {
        PreviewScriptCharacter psc = new PreviewScriptCharacter().Load(mc, imageLoader, loadedScript);
        psc.OnLoadMoreInfo += Psc_OnLoadMoreInfo;
        psc.OnDeleteCharacterClicked += PscOnDeleteCharacterClicked;
        psc.OnPopOutMoreInfo += Psc_OnPopOutMoreInfo;
        psc.OnDragOverMe += Psc_OnDragOverMe;
        psc.OnAddCharacter += Psc_OnAddCharacter;

        return psc;
    }

    private void Psc_OnAddCharacter(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        AddCharacterToScript(e.Value);
    }

    private void Psc_OnDragOverMe(object? sender, SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?> e)
    {
        DragOverPreview(e.Value1, e.Value2);
    }

    private void Psc_OnPopOutMoreInfo(object? sender, SimpleEventArgs<EditCharacter, string> e)
    {
        if (e.Value1 is INamedControl<UserControl> nc)
        {
            PopAction(nc);
        }
        else
        {
            PopAction(e.Value1, e.Value2);
        }

        EditPanel.Children.RemoveAll(EditPanel.Children.OfType<EditCharacter>().Where(ec => ec.LoadedCharacter == e.Value1.LoadedCharacter));
    }

    private void PscOnDeleteCharacterClicked(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        RemoveCharacterFromScript(e.Value);
    }

    private void Psc_OnLoadMoreInfo(object? sender, SimpleEventArgs<UserControl> e)
    {
        LoadEditControl(e.Value);
    }
    private void Meta_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Title = $"{LoadedScript?.Meta.Name ?? "New Script"} - Clockmaker Beta {App.BetaVersionNumber}";
    }

    private void ScriptTitle_OnAddCharacter(object? sender, SimpleEventArgs<UserControl> e)
    {
        AddNewCharacter(TeamEnum.Townsfolk);
    }

    private void ScriptTitle_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        RemoveCharacterFromScript(e.Value);
    }

    private void ScriptTitle_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
    {
        if (e.Value1 is INamedControl<UserControl> nc)
        {
            PopAction(nc);
        }
        else
        {
            PopAction(e.Value1, e.Value2);
        }
    }

    private void ScriptTitle_OnLoadEdit(object? sender, SimpleEventArgs<UserControl> e)
    {
        LoadEditControl(e.Value);
    }


    private void CheckForUpdatesMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            using HttpClient client = new();
            string newVersion = await client.GetStringAsync("https://clockmaker.pikcube.com/beta/version.txt");
            int result = string.CompareOrdinal(newVersion, App.BetaVersionNumber);
            if (result <= 0)
            {
                return;
            }

            string url = await client.GetStringAsync("https://clockmaker.pikcube.com/beta/installerUrl.txt");
            Task<byte[]> installerBytes = client.GetByteArrayAsync(url);

            ButtonResult msgBox = await MessageBoxManager.GetMessageBoxStandard("New Version",
                $"Version {newVersion} is available for download, would you like to update now?",
                ButtonEnum.YesNo).ShowWindowDialogAsync(this);
            if (msgBox != ButtonResult.Yes)
            {
                return;
            }

            foreach (MainWindow mw in App.Windows)
            {
                if (await mw.IsCancelCloseAsync())
                {
                    return;
                }
            }

            byte[] bytes = await installerBytes;

            string appdata = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clockmaker", "NewVersion");
            if (!Directory.Exists(appdata))
            {
                Directory.CreateDirectory(appdata);
            }

            string path = Path.Join(appdata, "setup.exe");
            await File.WriteAllBytesAsync(path, bytes);
            IStorageFile file = await StorageProvider.TryGetFileFromPathAsync(path) ?? throw new NoNullAllowedException("file can't be null");
            await Launcher.LaunchFileAsync(file);
            App.CloseAllWindows();
        });
    }

    private void OpenRecentMenuItem_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        OpenRecentMenuItem.Items.Clear();
        foreach (IStorageFile file in RecentlyOpenedFiles)
        {
            MenuItem mi = new()
            {
                Header = file.Name
            };
            mi.Click += (_, _) => TaskManager.ScheduleAsyncTask(async () =>
            {
                if (LoadedScript is null || LoadedScript.Characters.Count == 0)
                {
                    PathToOpenFile?.Dispose();
                    PathToOpenFile = file;
                    ClockFile.Dispose();

                    await LoadScriptFileAsync(file);
                }
                else
                {
                    await CreateAndShowAsync(file);
                }
            });
            OpenRecentMenuItem.Items.Add(mi);
        }

        if (OpenRecentMenuItem.Items.Count == 0)
        {
            OpenRecentMenuItem.Items.Add(new MenuItem
            {
                Header = "None",
                IsEnabled = false
            });
        }
    }

    private void Layoutable_OnLayoutUpdated(object? sender, EventArgs e)
    {
        UpdateResponsiveLayouts();
    }

    private void UpdateResponsiveLayouts()
    {
        bool isEditPanelEmpty = EditPanel.Children.Count == 0;
        CloseButton.IsVisible = !isEditPanelEmpty;
        PopOutButton.IsVisible = !isEditPanelEmpty;

        switch (App.DataStore.ColumnCount)
        {
            case 0 when isEditPanelEmpty:
            case 1 when isEditPanelEmpty:
                UpdateScriptColumnLayout();
                return;
            case 0 when Width < 1200:
            case 1:
                UpdateEditColumnLayout();
                return;
            case 0:
            case 2:
                UpdateTwoColumnLayout();
                return;
            default:
                App.DataStore.ColumnCount = 0;
                goto case 0;
        }
    }

    private void UpdateScriptColumnLayout()
    {
        ColumnDefinitions column = MainGridView.ColumnDefinitions;
        column[0].MinWidth = 600;
        column[0].Width = GridLength.Star;
        column[1].Width = new GridLength(0);
        column[2].MinWidth = 0;
        column[2].Width = new GridLength(0);
    }

    private void UpdateEditColumnLayout()
    {
        ColumnDefinitions column = MainGridView.ColumnDefinitions;
        column[0].MinWidth = 0;
        column[0].Width = new GridLength(0);
        column[1].Width = new GridLength(0);
        column[2].MinWidth = 600;
        column[2].Width = GridLength.Star;
    }

    private void UpdateTwoColumnLayout()
    {
        ColumnDefinitions column = MainGridView.ColumnDefinitions;
        column[0].MinWidth = 600;
        column[2].MinWidth = 600;
        if (column[0].Width.IsStar || column[0].Width.Value < 600)
        {
            column[0].Width = new GridLength(Width / 2);
            column[2].Width = GridLength.Star;
        }
        column[1].Width = new GridLength(10);

    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (IDelete d in EditPanel.Children.OfType<IDelete>())
        {
            d.Delete();
        }
        EditPanel.Children.Clear();
    }

    private void PopOutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        switch (EditPanel.Children.Count)
        {
            case 0:
                //How the hell did you even fire, this button isn't accessible with a child count of 0
                return;
            case 1:
                Control c = EditPanel.Children[0];
                EditPanel.Children.Clear();
                if (c is INamedControl<Control> nc)
                {
                    PopAction(nc);
                }
                else
                {
                    PopAction(c, c.GetType().Name);
                }
                return;
        }
    }

    private void DebugMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var printpreview = new CharacterSheetPreview(LoadedScript!, ImageLoader!);
        printpreview.Show(this);
    }
}