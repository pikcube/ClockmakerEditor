using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using Clockmaker0.Data;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0;

/// <summary>
/// Class for storing properties between app sessions.
/// </summary>
public sealed class PersistentDataStore : INotifyPropertyChanged
{
    [JsonIgnore]
    private int _columnCount;
    [JsonIgnore]
    private DefaultAction _previewActionDefault;
    [JsonIgnore]
    private ThemeEnum _theme;
    [JsonIgnore]
    private bool _isIdEditable;

    /// <summary>
    /// Toggle between one column layout and two column layout. Set it to 0 to adjust based on Window size
    /// </summary>
    [JsonProperty(nameof(ColumnCount))]
    public int ColumnCount
    {
        get => _columnCount;
        set => SetField(ref _columnCount, value);
    }

    /// <summary>
    /// The default action for clicking on the script preview button
    /// </summary>
    [JsonProperty(nameof(PreviewActionDefault))]
    public DefaultAction PreviewActionDefault
    {
        get => _previewActionDefault;
        set => SetField(ref _previewActionDefault, value);
    }

    /// <summary>
    /// The currently selected app theme
    /// </summary>
    [JsonProperty(nameof(Theme))]
    public ThemeEnum Theme
    {
        get => _theme;
        set => SetField(ref _theme, value);
    }

    /// <summary>
    /// True if the ID is visible for the user to mutate, False otherwise
    /// </summary>
    [JsonProperty(nameof(IsIdEditable))]
    public bool IsIdEditable
    {
        get => _isIdEditable;
        set => SetField(ref _isIdEditable, value);
    }

    /// <summary>
    /// Collection of Recently Opened Files. Add or Remove with the RegisterOpenFile and DeregisterOpenFile functions. Raises the RecentlyOpenFilesChanged event when a file is added or removed
    /// </summary>
    [JsonProperty(nameof(RecentlyOpenedFiles))]
    public TrackedList<string> RecentlyOpenedFiles { get; init; } = [];

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initialize the data store with default values
    /// </summary>
    public PersistentDataStore()
    {
        RecentlyOpenedFiles.ItemAdded += RecentlyOpenedFiles_ItemAdded;
        RecentlyOpenedFiles.ItemRemoved += RecentlyOpenedFiles_ItemRemoved;
        RecentlyOpenedFiles.OrderChanged += RecentlyOpenedFiles_OrderChanged;
    }


    /// <summary>
    /// Create a PersistantDataStore Object from the default file on disk. If no store exists, create a new one
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NoNullAllowedException"></exception>
    public static PersistentDataStore Load()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clockmaker");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        try
        {
            string filePath = Path.Combine(folder, "data.json");
            if (File.Exists(filePath))
            {
                return JsonConvert.DeserializeObject<PersistentDataStore>(File.ReadAllText(filePath)) ??
                       throw new NoNullAllowedException();
            }
            return new PersistentDataStore();
        }
        catch (Exception)
        {
            return new PersistentDataStore();
        }
    }

    /// <summary>
    /// Write the app data to the persistent data store
    /// </summary>
    /// <param name="store">The data store to save</param>
    public static void Save(PersistentDataStore store)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clockmaker");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string filePath = Path.Combine(folder, "data.json");
        File.WriteAllText(filePath, JsonConvert.SerializeObject(store));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Save(this);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void RecentlyOpenedFiles_OrderChanged(object? sender, ValueChangedArgs<TrackedList<string>> e)
    {
        OnPropertyChanged(nameof(RecentlyOpenedFiles));
    }

    private void RecentlyOpenedFiles_ItemRemoved(object? sender, ValueChangedArgs<string> e)
    {
        OnPropertyChanged(nameof(RecentlyOpenedFiles));
    }

    private void RecentlyOpenedFiles_ItemAdded(object? sender, ValueChangedArgs<string> e)
    {
        RecentlyOpenedFiles.ItemAdded -= RecentlyOpenedFiles_ItemAdded;
        RecentlyOpenedFiles.ItemRemoved -= RecentlyOpenedFiles_ItemRemoved;
        RecentlyOpenedFiles.OrderChanged -= RecentlyOpenedFiles_OrderChanged;
        int index = RecentlyOpenedFiles.IndexOf(e.NewValue);
        if (index != -1)
        {
            RecentlyOpenedFiles.MoveIndexOfToIndexOf(index, 0);
        }
        else
        {
            RecentlyOpenedFiles.Insert(0, e.NewValue);
        }

        while (RecentlyOpenedFiles.IndexOf(e.NewValue) != RecentlyOpenedFiles.LastIndexOf(e.NewValue))
        {
            RecentlyOpenedFiles.RemoveAt(RecentlyOpenedFiles.LastIndexOf(e.NewValue));
        }

        while (RecentlyOpenedFiles.Count > 10)
        {
            RecentlyOpenedFiles.RemoveAt(10);
        }
        RecentlyOpenedFiles.ItemAdded += RecentlyOpenedFiles_ItemAdded;
        RecentlyOpenedFiles.ItemRemoved += RecentlyOpenedFiles_ItemRemoved;
        RecentlyOpenedFiles.OrderChanged += RecentlyOpenedFiles_OrderChanged;
        OnPropertyChanged(nameof(RecentlyOpenedFiles));
    }
}