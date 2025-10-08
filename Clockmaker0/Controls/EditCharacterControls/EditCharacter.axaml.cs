using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Clockmaker0.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.EditCharacterControls;

public partial class EditCharacter : UserControl
{
    public MutableCharacter LoadedCharacter { get; private set; } = MutableCharacter.Default;
    private ScriptImageLoader ImageLoader { get; set; } = ScriptImageLoader.Default;
    private MutableBotcScript LoadedScript { get; set; } = new(BotcScript.Default, ScriptParse.GetOfficialCharacters);
    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;
    public event EventHandler<SimpleEventArgs<UserControl, string>>? OnPop;

    public EditCharacter()
    {
        InitializeComponent();
    }

    public void Load(MutableCharacter loadedCharacter, ScriptImageLoader loader, MutableBotcScript loadedScript, IImage icon)
    {
        LoadedCharacter = loadedCharacter;
        LoadedScript = loadedScript;
        ImageLoader = loader;
        ImagePicker.Load(loadedCharacter, loader, icon);
        ImagePicker.OnDelete += RaiseOnDelete;
        BasicCharacterInfo.Load(loadedCharacter, loadedScript);
        RemindersTab.Load(loadedCharacter);
        FirstNightOrderTab.Load(loadedCharacter, loadedScript, loader);
        FirstNightOrderTab.OnDelete += RaiseOnDelete;
        FirstNightOrderTab.OnPop += RaiseOnPop;
        OtherNightOrderTab.Load(loadedCharacter, loadedScript, loader);
        OtherNightOrderTab.OnDelete += RaiseOnDelete;
        OtherNightOrderTab.OnPop += RaiseOnPop;
        AppFeaturesTab.Load(loadedCharacter, loadedScript);
        JinxesTab.Load(loadedCharacter, loadedScript);
        AdvancedTab.Load(loadedCharacter, loadedScript, loader);

        if (ScriptParse.IsOfficial(loadedCharacter.Id))
        {
            ImagePicker.Lock();
            BasicCharacterInfo.IsEnabled = false;
            RemindersTab.IsEnabled = false;
            FirstNightOrderTab.Lock();
            OtherNightOrderTab.Lock();
            AppFeaturesTab.Lock();
            JinxesTab.IsEnabled = false;
            AdvancedTab.Lock();
            ReadOnlyLock.Load(ForkAsync, loadedCharacter.Id, loader);
            loader.OnFork += OnFork;
        }
        else
        {
            ReadOnlyLock.IsVisible = false;
            ReadOnlyLock.IsEnabled = false;
        }

        loadedScript.Characters.ItemRemoved += Characters_ItemRemoved;
    }

    private void RaiseOnPop(object? sender, SimpleEventArgs<UserControl, string> e)
    {
        OnPop?.Invoke(sender, e);
    }

    private void RaiseOnDelete(object? sender, SimpleEventArgs<MutableCharacter> e)
    {
        OnDelete?.Invoke(sender, e);
    }

    private void OnFork(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        if (e.NewValue.Id != LoadedCharacter.Id)
        {
            return;
        }

        ImagePicker.Unlock();
        BasicCharacterInfo.IsEnabled = true;
        RemindersTab.IsEnabled = true;
        FirstNightOrderTab.Unlock();
        OtherNightOrderTab.Unlock();
        AppFeaturesTab.Unlock();
        JinxesTab.IsEnabled = true;
        AdvancedTab.Unlock();
    }

    private async Task ForkAsync()
    {
        string newId = LoadedCharacter.Id + $"_{LoadedScript.Meta.Name.ToLower()}";
        MutableJinx[] jinxes = [.. LoadedScript.Jinxes.Where(j => j.Child == LoadedCharacter.Id)];
        MutableJinx[] officialJinxes = [.. jinxes.Where(j => ScriptParse.IsOfficial(j.Parent))];
        MutableJinx[] unofficialJinxes = [.. jinxes.Where(j => !ScriptParse.IsOfficial(j.Parent))];
        ForkEnum resolution = await ResolveJinxes(officialJinxes);
        if (resolution is ForkEnum.None)
        {
            return;
        }
        switch (resolution)
        {
            case ForkEnum.Manual:
            case ForkEnum.Empty:
                await LoadedCharacter.ForkAsync(LoadedScript, ImageLoader, newId);
                break;
            case ForkEnum.SwapOwner:
                await LoadedCharacter.ForkAsync(LoadedScript, ImageLoader, newId);
                LoadedScript.Jinxes.AddRange(officialJinxes.Select(j => new MutableJinx(j.Rule, LoadedCharacter.Id, j.Parent)));
                break;
            case ForkEnum.RecurseFork:
                await ForkRecursiveAsync(LoadedCharacter, LoadedScript, ImageLoader);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        LoadedScript.SwapJinxes();
        foreach (MutableJinx jinx in unofficialJinxes)
        {
            jinx.Child = LoadedCharacter.Id;
        }
    }

    private async Task<ForkEnum> ResolveJinxes(MutableJinx[] jinxes)
    {
        if (jinxes.Length == 0)
        {
            return ForkEnum.Empty;
        }
        string swapOwner = $"Yes: Make {LoadedCharacter.Name} the owner of their jinxes";
        string recurseFork = $"Yes: Fork all characters that own a jinx with {LoadedCharacter.Name}";
        const string manual = "No: I will preserve these jinxes manually";
        string contentMessage = $"The following characters own a jinx with {LoadedCharacter.Name}:\n" +
                                $"{string.Join(' ', jinxes.Select(j => LoadedScript.Characters.Single(c => c.Id == j.Parent).Name))}\n" +
                                "Would you like to automatically preserve these jinxes?";
        IMsBox<string> msgBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ButtonDefinitions =
            [
                new ButtonDefinition { Name = swapOwner, IsDefault = true },
                new ButtonDefinition { Name = recurseFork },
                new ButtonDefinition { Name = manual, },
                new ButtonDefinition { Name = "Cancel", IsCancel = true, }
            ],
            CanResize = false,
            CloseOnClickAway = false,
            ContentHeader = "",
            ContentMessage = contentMessage,
            ContentTitle = "Preserve All Jinxes",
            SizeToContent = SizeToContent.WidthAndHeight
        });

        string s = TopLevel.GetTopLevel(this) is Window window
           ? await msgBox.ShowWindowDialogAsync(window)
            : await msgBox.ShowAsPopupAsync(this);

        if (s == swapOwner)
        {
            return ForkEnum.SwapOwner;
        }

        if (s == recurseFork)
        {
            return ForkEnum.RecurseFork;
        }

        return s == manual ? ForkEnum.Manual : ForkEnum.None;
    }

    private static async Task ForkRecursiveAsync(MutableCharacter loadedCharacter, MutableBotcScript loadedScript, ScriptImageLoader loader)
    {
        string newId = loadedCharacter.Id + $"_{loadedScript.Meta.Name.ToLower()}";
        MutableJinx[] jinxes = [.. loadedScript.Jinxes.Where(j => j.Child == loadedCharacter.Id)];
        MutableJinx[] officialJinxes = [.. jinxes.Where(j => ScriptParse.IsOfficial(j.Parent))];
        MutableJinx[] unofficialJinxes = [.. jinxes.Where(j => !ScriptParse.IsOfficial(j.Parent))];
        await loadedCharacter.ForkAsync(loadedScript, loader, newId);
        foreach (MutableCharacter c in loadedScript.Characters.Where(c => officialJinxes.Select(j => j.Parent).Contains(c.Id)))
        {
            await ForkRecursiveAsync(c, loadedScript, loader);
        }
        foreach (MutableJinx jinx in unofficialJinxes)
        {
            jinx.Child = loadedCharacter.Id;
        }
    }


    private void Characters_ItemRemoved(object? sender, ValueChangedArgs<MutableCharacter> e)
    {
        if (e.NewValue == LoadedCharacter)
        {
            Delete();
        }
    }

    public void Delete()
    {
        ImagePicker.Delete();
        BasicCharacterInfo.Delete();
        RemindersTab.Delete();
        JinxesTab.Delete();
        FirstNightOrderTab.Delete();
        OtherNightOrderTab.Delete();
        AppFeaturesTab.Delete();
        AdvancedTab.Delete();
        ReadOnlyLock.Delete();
    }
}