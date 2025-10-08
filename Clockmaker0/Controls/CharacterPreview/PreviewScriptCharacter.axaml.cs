using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Clockmaker0.Controls.EditCharacterControls;
using Clockmaker0.Data;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.CharacterPreview;

public partial class PreviewScriptCharacter : UserControl, IDelete
{
    public MutableCharacter? LoadedCharacter { get; private set; } = MutableCharacter.Default;

    private ScriptImageLoader? ImageLoader { get; set; } = ScriptImageLoader.Default;

    private bool? IsImageLoadingEnabled { get; set; } = true;

    private MutableBotcScript? LoadedScript { get; set; } = BotcScript.Default.ToMutable();

    public event EventHandler<SimpleEventArgs<UserControl>>? OnLoadMoreInfo;
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDeleteCharacterClicked;
    public event EventHandler<SimpleEventArgs<UserControl>>? OnUnloadMoreInfoWindow;
    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPopOutMoreInfo;
    public event EventHandler<SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>>? OnDragOverMe;
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnAddCharacter;


    private EditCharacter? EditControl { get; set; }

    public PreviewScriptCharacter()
    {
        InitializeComponent();

        IsEnabled = false;
    }

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
        App.OnKeyChanged += App_OnKeyChanged;
        PointerEntered += PreviewScriptCharacter_PointerEntered;
        PointerExited += PreviewScriptCharacter_PointerExited;
        LoadedCharacter.PropertyChanged += Character_PropertyChanged;

        DragHandle.PointerMoved += ActionButton_PointerMoved;
        DragHandle.Cursor = new Cursor(StandardCursorType.Hand);
        BorderBrush = new SolidColorBrush(Colors.LightGreen);

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        return this;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (LoadedScript is null || LoadedCharacter is null || e.Data.Get("character") is not MutableCharacter dragCharacter || e.Data.Get("script") is not MutableBotcScript script)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (script != LoadedScript)
        {
            e.DragEffects = DragDropEffects.Copy;
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedCharacter,
                Value2 = e.Data.Get("obj") as PreviewScriptCharacter
            });
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        bool allowDrag = dragCharacter.Team == LoadedCharacter.Team;
        if (allowDrag)
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedCharacter,
                Value2 = e.Data.Get("obj") as PreviewScriptCharacter
            });
            return;
        }

        int indexOfMe = LoadedScript.Characters.IndexOf(LoadedCharacter);
        int firstIndex = LoadedScript.Characters.FindIndex(c => c.Team == dragCharacter.Team);
        int lastIndex = LoadedScript.Characters.FindLastIndex(c => c.Team == dragCharacter.Team);

        int firstDiff = Math.Abs(firstIndex - indexOfMe);
        int lastDiff = Math.Abs(lastIndex - indexOfMe);

        if (firstDiff < lastDiff)
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedScript.Characters[firstIndex],
                Value2 = e.Data.Get("obj") as PreviewScriptCharacter
            });
        }
        else
        {
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = LoadedScript.Characters[lastIndex],
                Value2 = e.Data.Get("obj") as PreviewScriptCharacter
            });
        }
    }

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

        if (LoadedCharacter is null)
        {
            return;
        }

        if (LoadedScript is null)
        {
            return;
        }


        if (e.Data.Get("character") is not MutableCharacter droppedCharacter)
        {
            return;
        }

        if (droppedCharacter == LoadedCharacter)
        {
            return;
        }

        if (e.Data.Get("script") is not MutableBotcScript script)
        {
            return;
        }

        if (script != LoadedScript)
        {
            if (e.Data.Get("loader") is not ScriptImageLoader loader)
            {
                return;
            }

            if (ImageLoader is null)
            {
                return;
            }

            TaskManager.ScheduleAsyncTask(async () => await App.CopyCharacterAsync(droppedCharacter, loader, LoadedScript, LoadedCharacter,
                ImageLoader, script.Jinxes, c => OnAddCharacter?.Invoke(this, new SimpleEventArgs<MutableCharacter>(c))));
            return;
        }

        LoadedScript?.Characters.MoveTo(droppedCharacter, LoadedCharacter);
        e.Handled = true;
    }

    private void ActionButton_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (LoadedScript is null)
        {
            return;
        }
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (ImageLoader is null)
        {
            return;

        }

        DataObject data = new();
        data.Set("obj", this);
        data.Set("character", LoadedCharacter ?? throw new NoNullAllowedException());
        data.Set("immutableCharacter", LoadedCharacter.ToImmutable(LoadedScript.Jinxes));
        data.Set("script", LoadedScript);
        data.Set("loader", ImageLoader);
        data.Set(DataFormats.Text, JsonConvert.SerializeObject(LoadedCharacter.ToImmutable(LoadedScript.Jinxes), Formatting.Indented));
        TaskManager.ScheduleTask(async () =>
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move | DragDropEffects.Copy);
            OnDragOverMe?.Invoke(this, new SimpleEventArgs<MutableCharacter?, PreviewScriptCharacter?>
            {
                Value1 = null,
                Value2 = null
            });
        });
    }

    private void ClockmakerExtensions_OnFork(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        if (LoadedCharacter?.Id != e.NewValue.Id)
        {
            return;
        }

        if (ImageLoader is not null)
        {
            ImageLoader.OnFork -= ClockmakerExtensions_OnFork;
        }

        Unlock();
    }

    void IDelete.Delete() => Delete();

    public EditCharacter? Delete()
    {
        IsEnabled = false;
        NameTextBox.TextChanged -= NameTextBox_TextChanged;
        AbilityTextBox.TextChanged -= AbilityTextBox_TextChanged;
        ActionButton.Click -= ActionButtonOnClick;
        App.OnKeyChanged -= App_OnKeyChanged;
        PointerEntered -= PreviewScriptCharacter_PointerEntered;
        PointerExited -= PreviewScriptCharacter_PointerExited;
        if (LoadedCharacter is not null)
        {
            LoadedCharacter.PropertyChanged -= Character_PropertyChanged;
        }

        EditControl?.Delete();
        ActionButton.Cursor?.Dispose();

        return EditControl;
    }

    private void Character_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedCharacter.Name):
                NameTextBox.Text = LoadedCharacter?.Name;
                NameTextBlock.Text = LoadedCharacter?.Name;
                break;
            case nameof(LoadedCharacter.Ability):
                AbilityTextBox.Text = LoadedCharacter?.Ability;
                AbilityTextBlock.Text = LoadedCharacter?.Ability;
                break;
            case nameof(LoadedCharacter.Team):
                ImageLoader?.ReloadDefault();
                break;
        }
    }

    private void AbilityTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (LoadedCharacter is null)
        {
            return;
        }
        LoadedCharacter.Ability = AbilityTextBox.Text ?? "";
    }

    private void NameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (LoadedCharacter is null)
        {
            return;
        }
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

    public async Task LoadImagesAsync()
    {
        if (IsImageLoadingEnabled is not true)
        {
            return;
        }

        if (LoadedCharacter is null)
        {
            return;
        }

        if (ImageLoader is null)
        {
            return;
        }

        CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0);
        ImageLoader.ReloadImage += ImageLoader_ReloadImage;
    }

    private void ImageLoader_ReloadImage(object? sender, KeyArgs e)
    {
        if (LoadedCharacter is null)
        {
            return;
        }

        if (ImageLoader is null)
        {
            return;
        }
        if (LoadedCharacter.Image.FirstOrDefault() == e.Key || e.Key is null)
        {
            TaskManager.ScheduleAsyncTask(async () => { CharacterImage.Source = await ImageLoader.GetImageAsync(LoadedCharacter, 0); });
        }
    }

    private void PreviewScriptCharacter_PointerExited(object? sender, PointerEventArgs e)
    {
        ActionButton.Content = IsShiftModeEnabled() ? "X" : "ⓘ";
    }

    private void PreviewScriptCharacter_PointerEntered(object? sender, PointerEventArgs e)
    {
        ActionButton.Content = IsShiftModeEnabled() ? "X" : "ⓘ";
    }

    private void App_OnKeyChanged(object? sender, App.KeyEventArgs e)
    {
        ActionButton.Content = IsShiftModeEnabled() ? "X" : "ⓘ";
    }

    private void ActionButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (IsShiftModeEnabled())
        {
            if (LoadedCharacter != null)
            {
                OnDeleteCharacterClicked?.Invoke(this, new SimpleEventArgs<MutableCharacter>(LoadedCharacter));
            }
        }
        else
        {
            LoadMoreInfo();
        }
    }

    public void LoadMoreInfo()
    {
        if (EditControl is null)
        {
            EditControl = new EditCharacter();
            if (LoadedCharacter != null && ImageLoader != null && LoadedScript != null)
            {
                EditControl.Load(LoadedCharacter, ImageLoader, LoadedScript, CharacterImage.Source ?? throw new NoNullAllowedException());
                EditControl.OnDelete += EditControl_OnDelete;
                EditControl.OnPop += EditControl_OnPop;
            }
        }

        OnLoadMoreInfo?.Invoke(this, new SimpleEventArgs<UserControl> { Value = EditControl });
    }

    private void EditControl_OnPop(object? sender, SimpleEventArgs<UserControl, string> e)
    {
        OnPopOutMoreInfo?.Invoke(sender, e);
    }

    private void EditControl_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDeleteCharacterClicked?.Invoke(sender, e);
    }

    private bool IsShiftModeEnabled() =>
        App.IsKeyDown(Key.LeftShift, Key.RightShift) &&
        App.IsKeyDown(Key.LeftCtrl, Key.RightCtrl) &&
        ActionButton.IsPointerOver;

    private void ExpandMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        LoadMoreInfo();
    }

    private void PopOutMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedCharacter is null || ImageLoader is null || LoadedScript is null)
        {
            return;
        }

        EditCharacter edit = new();
        edit.Load(LoadedCharacter, ImageLoader, LoadedScript, CharacterImage.Source ?? throw new NoNullAllowedException());
        edit.OnDelete += EditControl_OnDelete;
        edit.OnPop += EditControl_OnPop;
        OnPopOutMoreInfo?.Invoke(this, new SimpleEventArgs<UserControl, string>(edit, $"Edit {LoadedCharacter.Name}"));
        if (EditControl is not null)
        {
            OnUnloadMoreInfoWindow?.Invoke(this, new SimpleEventArgs<UserControl>(EditControl));
        }

    }

    private void DeleteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LoadedCharacter != null)
        {
            OnDeleteCharacterClicked?.Invoke(this, new SimpleEventArgs<MutableCharacter>(LoadedCharacter));
        }
    }
}