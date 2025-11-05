using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Clockmaker0.Controls.EditCharacterControls;
using Clockmaker0.Data;
using ImageMagick;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Clockmaker0.Controls.CharacterPreview;

/// <summary>
/// Control for viewing a character on the character sheet
/// </summary>
public partial class PreviewScriptCharacter : UserControl, IDelete
{
    /// <summary>
    /// The currently Loaded Character Data
    /// </summary>
    public MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;

    /// <summary>
    /// The current script's image loader
    /// </summary>
    public ScriptImageLoader ImageLoader { get; private set; } = ScriptImageLoader.Default;

    /// <summary>
    /// The currently loaded script
    /// </summary>
    public MutableBotcScript LoadedScript { get; private set; } = BotcScript.Default.ToMutable();

    /// <summary>
    /// Raised when the user wants to open the MoreInfo window on the right hand side
    /// </summary>
    public event EventHandler<SimpleEventArgs<UserControl>>? OnLoadMoreInfo;
    /// <summary>
    /// Raised when the user has indicated their intention to delete the character
    /// </summary>
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDeleteCharacterClicked;
    /// <summary>
    /// Raised when the user wants to pop out the more info window into a separate window
    /// </summary>
    public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPopOutMoreInfo;
    /// <summary>
    /// Raised when a control is dragged over this control
    /// </summary>
    public event EventHandler<SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>>? OnDragOverMe;
    /// <summary>
    /// Raised when a character is added to the script
    /// </summary>
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnAddCharacter;

    /// <inheritdoc />
    public PreviewScriptCharacter()
    {
        InitializeComponent();

        IsEnabled = false;
    }

    /// <summary>
    /// Load the current character data into the control. May only be called once
    /// </summary>
    /// <param name="character">The character to load</param>
    /// <param name="imageLoader">The script's image loader</param>
    /// <param name="loadedScript">The script containing the character</param>
    /// <returns></returns>
    public PreviewScriptCharacter Load(MutableCharacter character, ScriptImageLoader imageLoader, MutableBotcScript loadedScript)
    {
        LoadedCharacter = character;
        ImageLoader = imageLoader;
        LoadedScript = loadedScript;
        IsEnabled = true;
        NameTextBox.Text = character.Name;
        NameTextBlock.Text = character.Name;
        AbilityTextBox.Text = character.Ability;
        AbilityTextBlock.Text = character.Ability;
        Background = Brushes.Transparent;
        LoadedCharacter.OnDelete += LoadedCharacter_OnDelete;


        if (ScriptParse.IsOfficial(LoadedCharacter.Id))
        {
            Lock();
            imageLoader.OnFork += ClockmakerExtensions_OnFork;
        }
        else
        {
            Unlock();
        }

        NameTextBox.TextChanged += NameTextBox_TextChanged;
        AbilityTextBox.TextChanged += AbilityTextBox_TextChanged;
        ActionButton.Click += ActionButtonOnClick;
        ActionButton.Cursor = new Cursor(StandardCursorType.Hand);
        LoadedCharacter.PropertyChanged += Character_PropertyChanged;

        DragHandle.PointerMoved += ActionButton_PointerMoved;
        DragHandle.Cursor = new Cursor(StandardCursorType.Hand);
        BorderBrush = new SolidColorBrush(Colors.LightGreen);

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        App.DataStore.PropertyChanged += DataStore_PropertyChanged;

        return this;
    }

    private void DataStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(App.DataStore.PreviewActionDefault):
                ActionButton.Content = App.DataStore.PreviewActionDefault switch
                {
                    DefaultAction.None => "ⓘ",
                    DefaultAction.OpenInCurrentWindow => "ⓘ",
                    DefaultAction.OpenInNewWindow => "⮺",
                    DefaultAction.DeleteItem => "✕",
                    _ => "ⓘ"
                };
                return;
        }
    }

    private void LoadedCharacter_OnDelete(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        Delete();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is not CustomDataTransfer<PreviewDrag> cdt)
        {
            return;
        }

        PreviewDrag data = cdt.Value;

        if (data.LoadedScript != LoadedScript)
        {
            e.DragEffects = DragDropEffects.Copy;
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedCharacter,
                Value2 = data.Preview
            });
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        bool allowDrag = data.LoadedCharacter.Team == LoadedCharacter.Team;
        if (allowDrag)
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedCharacter,
                Value2 = data.Preview
            });
            return;
        }

        int indexOfMe = LoadedScript.Characters.IndexOf(LoadedCharacter);
        int firstIndex = LoadedScript.Characters.FindIndex(c => c.Team == data.LoadedCharacter.Team);
        int lastIndex = LoadedScript.Characters.FindLastIndex(c => c.Team == data.LoadedCharacter.Team);

        int firstDiff = Math.Abs(firstIndex - indexOfMe);
        int lastDiff = Math.Abs(lastIndex - indexOfMe);

        if (firstDiff < lastDiff)
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedScript.Characters[firstIndex],
                Value2 = data.Preview
            });
        }
        else
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedScript.Characters[lastIndex],
                Value2 = data.Preview
            });
        }
    }

    /// <summary>
    /// Adjusts the appearance of the control based on the current drag and drop. Passing a null character resets the control to the "no drag" state
    /// </summary>
    /// <param name="mc"></param>
    /// <param name="dropAfter"></param>
    public void ReactToDragOver(MutableCharacter? mc, bool dropAfter)
    {
        if (mc is null || mc != LoadedCharacter)
        {
            BorderThickness = new Thickness(0);
            return;
        }

        const double thickness = 4;
        BorderThickness = dropAfter ? new Thickness(0, 0, 0, thickness) : new Thickness(0, thickness, 0, 0);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
        {
            Value1 = null,
            Value2 = null,
        });

        if (e.DataTransfer is not CustomDataTransfer<PreviewDrag> cdt)
        {
            return;
        }

        PreviewDrag data = cdt.Value;


        if (data.LoadedCharacter == LoadedCharacter)
        {
            return;
        }

        if (data.LoadedScript != LoadedScript)
        {
            TaskManager.ScheduleAsyncTask(async () => await CopyCharacterAsync(data.LoadedCharacter, data.Loader, LoadedScript, LoadedCharacter,
                ImageLoader, data.LoadedScript.Jinxes, c => OnAddCharacter?.Invoke(this, new SimpleEventArgs<MutableCharacter>(c))));
            return;
        }

        LoadedScript.Characters.MoveTo(data.LoadedCharacter, LoadedCharacter);
        e.Handled = true;
    }

    private static async Task CopyCharacterAsync(MutableCharacter originalCharacter, ScriptImageLoader originalLoader, MutableBotcScript targetScript, MutableCharacter loadedCharacter, ScriptImageLoader targetLoader, TrackedList<MutableJinx> allJinxes, Action<MutableCharacter> addFunction)
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

    private void ActionButton_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        PreviewDrag previewDrag = new(this);
        string json = JsonConvert.SerializeObject(LoadedCharacter.ToImmutable(LoadedScript.Jinxes), Formatting.Indented);

        CustomDataTransfer<PreviewDrag> cdt = new(previewDrag, json);

        TaskManager.ScheduleTask(async () =>
        {
            await DragDrop.DoDragDropAsync(e, cdt, DragDropEffects.Move | DragDropEffects.Copy);
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = null,
                Value2 = null
            });
        });
    }

    private void ClockmakerExtensions_OnFork(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        if (LoadedCharacter.Id != e.NewValue.Id)
        {
            return;
        }

        ImageLoader.OnFork -= ClockmakerExtensions_OnFork;

        Unlock();
    }

    void IDelete.Delete() => Delete();

    /// <summary>
    /// Raised when a character is deleted
    /// </summary>
    private void Delete()
    {
        IsEnabled = false;
        NameTextBox.TextChanged -= NameTextBox_TextChanged;
        AbilityTextBox.TextChanged -= AbilityTextBox_TextChanged;
        ActionButton.Click -= ActionButtonOnClick;
        LoadedCharacter.PropertyChanged -= Character_PropertyChanged;
        ActionButton.Cursor?.Dispose();
    }

    private void Character_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedCharacter.Name):
                NameTextBox.Text = LoadedCharacter.Name;
                NameTextBlock.Text = LoadedCharacter.Name;
                break;
            case nameof(LoadedCharacter.Ability):
                AbilityTextBox.Text = LoadedCharacter.Ability;
                AbilityTextBlock.Text = LoadedCharacter.Ability;
                break;
            case nameof(LoadedCharacter.Team):
                ImageLoader.ReloadDefault();
                break;
        }
    }

    private void AbilityTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedCharacter.Ability = AbilityTextBox.Text ?? "";
    }

    private void NameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadedCharacter.Name = NameTextBox.Text ?? "";
    }

    private void Unlock()
    {
        NameTextBox.IsReadOnly = false;
        AbilityTextBox.IsReadOnly = false;
        NameTextBox.IsVisible = true;
        AbilityTextBox.IsVisible = true;
        NameTextBlock.IsVisible = false;
        AbilityTextBlock.IsVisible = false;

    }

    private void Lock()
    {
        NameTextBox.IsReadOnly = true;
        AbilityTextBox.IsReadOnly = true;
        NameTextBox.IsVisible = false;
        AbilityTextBox.IsVisible = false;
        NameTextBlock.IsVisible = true;
        AbilityTextBlock.IsVisible = true;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(LoadImagesAsync);
    }

    private async Task LoadImagesAsync()
    {
        CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0);
        ImageLoader.ReloadImage += ImageLoader_ReloadImage;
    }

    private void ImageLoader_ReloadImage(object? sender, KeyArgs e)
    {
        if (LoadedCharacter.Image.FirstOrDefault() == e.Key || e.Key is null)
        {
            TaskManager.ScheduleAsyncTask(async () => { CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0); });
        }
    }

    private void ActionButtonOnClick(object? sender, RoutedEventArgs e)
    {
        switch (App.DataStore.PreviewActionDefault)
        {
            case DefaultAction.None:
            case DefaultAction.OpenInCurrentWindow:
                LoadMoreInfo();
                break;
            case DefaultAction.OpenInNewWindow:
                PopOutMoreInfo();
                break;
            case DefaultAction.DeleteItem:
                Delete();
                break;
            default:
                App.DataStore.PreviewActionDefault = DefaultAction.None;
                goto case DefaultAction.None;
        }
    }

    private void LoadMoreInfo()
    {
        EditCharacter editControl = new();
        editControl.Load(LoadedCharacter, ImageLoader, LoadedScript, CharacterImage.Source ?? throw new NoNullAllowedException());
        editControl.OnDelete += EditControl_OnDelete;
        editControl.OnPop += EditControl_OnPop;

        OnLoadMoreInfo?.Invoke(this, new SimpleEventArgs<UserControl> { Value = editControl });
    }

    private void EditControl_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
    {
        OnPopOutMoreInfo?.Invoke(sender, e);
    }

    private void EditControl_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDeleteCharacterClicked?.Invoke(sender, e);
    }

    private void ExpandMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadMoreInfo();
    }

    private void PopOutMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        PopOutMoreInfo();
    }

    private void PopOutMoreInfo()
    {
        EditCharacter edit = new();
        edit.Load(LoadedCharacter, ImageLoader, LoadedScript, CharacterImage.Source ?? throw new NoNullAllowedException());
        edit.OnDelete += EditControl_OnDelete;
        edit.OnPop += EditControl_OnPop;
        OnPopOutMoreInfo?.Invoke(this, new SimpleEventArgs<EditCharacter, string>(edit, $"Edit {LoadedCharacter.Name}"));
    }

    private void DeleteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        OnDeleteCharacterClicked?.Invoke(this, new SimpleEventArgs<MutableCharacter>(LoadedCharacter));
    }
}