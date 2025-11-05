using System;
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

/// <summary>
/// The full night order view
/// </summary>
public partial class NightOrderView : UserControl, IOnDelete, IOnPop
{
    private TrackedList<MutableCharacter> LoadedList { get; set; } = [];
    private MutableBotcScript LoadedScript { get; set; } = BotcScript.Default.ToMutable();
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;

    /// <inheritdoc />
    public event EventHandler<SimpleEventArgs<EditCharacter, string>>? OnPop;
    private Func<MutableCharacter, string?> GetReminder { get; set; } = _ => null;

    /// <inheritdoc />
    public NightOrderView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the current night order
    /// </summary>
    /// <param name="loadedList">The night order list</param>
    /// <param name="loadedScript">The loaded script</param>
    /// <param name="loader">The image loader</param>
    /// <param name="getReminder">A function that gets the reminder text for a given character</param>
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

    /// <summary>
    /// Called when the control is deleted
    /// </summary>
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

    private void InitTreeViews()
    {
        AbstractNightTreeView.ItemsSource = LoadedList.Select(CreateNightOrderPreview);
    }

    private void RegenerateTreeViews()
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

    private void ReorderTreeViews()
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
            OnPop?.Invoke(this, new SimpleEventArgs<EditCharacter, string>(edit, $"Edit {character.Name}"));

        });
    }

    private void Edit_OnPop(object? sender, SimpleEventArgs<EditCharacter, string> e)
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

        if (sender is not NightOrderPreview nop)
        {
            return;
        }

        CustomDataTransfer<NightOrderDrag> cdt = new(new NightOrderDrag(nop, list), JsonConvert.SerializeObject(character.ToImmutable(LoadedScript.Jinxes), Formatting.Indented));

        TaskManager.ScheduleAsyncTask(async () =>
        {
            await DragDrop.DoDragDropAsync(e, cdt, DragDropEffects.Move);
            DragOverMe(null, null);
        });
    }

    private void OnDragOver(DragEventArgs e, MutableCharacter owner, TrackedList<MutableCharacter> nightOrder)
    {
        if (e.DataTransfer is not CustomDataTransfer<NightOrderDrag> cdt)
        {
            return;
        }

        NightOrderDrag data = cdt.Value;

        if (data.List != nightOrder)
        {
            DragOverMe(null, null);
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        DragOverMe(owner, data.Preview);

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
        if (e.DataTransfer is not CustomDataTransfer<NightOrderDrag> cdt)
        {
            return;
        }


        NightOrderDrag data = cdt.Value;

        if (data.LoadedCharacter == owner || data.List != nightOrder)
        {
            return;
        }

        data.List.MoveTo(data.LoadedCharacter, owner);
        e.Handled = true;
    }
}