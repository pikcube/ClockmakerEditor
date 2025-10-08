using System;
using System.Data;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Clockmaker0.Controls.CharacterImport;
using Clockmaker0.Data;
using Newtonsoft.Json;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Tabs;

public partial class NightOrderView : UserControl
{
    private TrackedList<MutableCharacter> LoadedList { get; set; } = [];
    protected MutableBotcScript LoadedScript { get; private set; } = BotcScript.Default.ToMutable();
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPop;
    private Func<MutableCharacter, string?> GetReminder { get; set; } = _ => null;
    public NightOrderView()
    {
        InitializeComponent();
    }

    public void Load(TrackedList<MutableCharacter> loadedList, MutableBotcScript loadedScript, ScriptImageLoader loader, Func<MutableCharacter, string?> getReminder)
    {
        LoadedList = loadedList;
        LoadedScript = loadedScript;
        Loader = loader;
        GetReminder = getReminder;

        InitTreeViews();

        LoadedList.ItemAdded += LoadedList_ItemAdded;
        LoadedList.ItemRemoved += LoadedList_ItemRemoved;
        LoadedList.OrderChanged += LoadedList_OrderChanged;
    }

    public void Delete()
    {
        LoadedList.ItemAdded -= LoadedList_ItemAdded;
        LoadedList.ItemRemoved -= LoadedList_ItemRemoved;
        LoadedList.OrderChanged -= LoadedList_OrderChanged;
    }


    private void LoadedList_OrderChanged(object? sender, ValueChangedArgs<TrackedList<MutableCharacter>> e)
    {
        ReorderTreeViews();
    }

    private void LoadedList_ItemRemoved(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        RegenerateTreeViews();
    }

    private void LoadedList_ItemAdded(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        RegenerateTreeViews();
    }

    public void InitTreeViews()
    {
        AbstractNightTreeView.ItemsSource = LoadedList.Select(CreateNightOrderPreview);
    }

    public void RegenerateTreeViews()
    {
        AbstractNightTreeView.ItemsSource = LoadedList.Select(GetOrCreatePreview);
    }

    private NightOrderPreview GetOrCreatePreview(MutableCharacter character)
    {
        return AbstractNightTreeView.Items.OfType<NightOrderPreview>().FirstOrDefault(nop => nop.LoadedCharacter == character) ?? CreateNightOrderPreview(character);
    }

    private NightOrderPreview CreateNightOrderPreview(MutableCharacter character)
    {
        NightOrderPreview item = new()
        {
            BorderBrush = new SolidColorBrush(Colors.LightGreen)
        };
        item.DoubleTapped += (_, _) => Item_DoubleTapped(character);
        DragDrop.SetAllowDrop(item, true);
        item.Load(character, Loader, GetReminder(character));
        item.AddHandler(DragDrop.DropEvent, (_, args) => OnDrop(args, character, LoadedList));
        item.AddHandler(DragDrop.DragOverEvent, (_, args) => OnDragOver(args, character, LoadedList));
        item.PointerMoved += (sender, args) => NightListPointerMoved(character, LoadedList, sender, args);
        return item;
    }

    public void ReorderTreeViews()
    {
        AbstractNightTreeView.ItemsSource = AbstractNightTreeView.ItemsSource?.OfType<NightOrderPreview>()
            .OrderBy(nop => LoadedList.IndexOf(nop.LoadedCharacter));
    }

    private void Item_DoubleTapped(MutableCharacter character)
    {
        if (character.Team == TeamEnum.Special)
        {
            return;
        }
        TaskManager.ScheduleAsyncTask(async () =>
        {
            EditCharacter edit = new();
            edit.Load(character, Loader, LoadedScript, await Loader.GetImageAsync(character, 0));
            edit.OnDelete += Edit_OnDelete;
            edit.OnPop += Edit_OnPop;
            OnPop?.Invoke(this, new SimpleEventArgs<UserControl, string>(edit, $"Edit {character.Name}"));

        });
    }

    private void Edit_OnPop(object? sender, SimpleEventArgs<UserControl, string> e)
    {
        OnPop?.Invoke(this, e);
    }

    private void Edit_OnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(this, e);
    }

    private void NightListPointerMoved(MutableCharacter character, TrackedList<MutableCharacter> list, object? sender, PointerEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
        {
            return;
        }

        DataObject data = new();
        data.Set("obj", sender ?? "");
        data.Set("character", character ?? throw new NoNullAllowedException());
        data.Set("list", list);
        data.Set(DataFormats.Text, JsonConvert.SerializeObject(character.ToImmutable(LoadedScript.Jinxes), Formatting.Indented));
        TaskManager.ScheduleAsyncTask(async () =>
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            DragOverMe(null, null);
        });
    }

    private void OnDragOver(DragEventArgs e, MutableCharacter owner, TrackedList<MutableCharacter> nightOrder)
    {
        if (e.Data.Get("character") is not MutableCharacter || e.Data.Get("list") is not TrackedList<MutableCharacter> list || list != nightOrder)
        {
            DragOverMe(null, null);
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        DragOverMe(owner, e.Data.Get("obj") as NightOrderPreview);

    }

    private void DragOverMe(MutableCharacter? owner, NightOrderPreview? get)
    {
        if (AbstractNightTreeView.Items.Contains(get))
        {
            bool dropAfter = false;
            foreach (NightOrderPreview psc in AbstractNightTreeView.Items.OfType<NightOrderPreview>())
            {
                ReactToDragOver(owner, dropAfter, psc);
                if (psc == get)
                {
                    dropAfter = true;
                }
            }
        }
        else
        {
            foreach (NightOrderPreview psc in AbstractNightTreeView.Items.OfType<NightOrderPreview>())
            {
                ReactToDragOver(null, false, psc);
            }
        }
    }

    private static void ReactToDragOver(MutableCharacter? mc, bool dropAfter, NightOrderPreview psc)
    {
        if (mc is null || mc != psc.LoadedCharacter)
        {
            psc.BorderThickness = new Thickness(0);
            return;
        }

        const double thickness = 4;
        psc.BorderThickness = dropAfter ? new Thickness(0, 0, 0, thickness) : new Thickness(0, thickness, 0, 0);
    }

    private static void OnDrop(DragEventArgs e, MutableCharacter owner, TrackedList<MutableCharacter> nightOrder)
    {
        if (e.Data.Get("character") is not MutableCharacter droppedCharacter || droppedCharacter == owner)
        {
            return;
        }

        if (e.Data.Get("list") is not TrackedList<MutableCharacter> list || list != nightOrder)
        {
            return;
        }

        list.MoveTo(droppedCharacter, owner);
        e.Handled = true;
    }
}