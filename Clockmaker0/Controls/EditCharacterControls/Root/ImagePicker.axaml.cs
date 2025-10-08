using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Clockmaker0.Data;
using ImageMagick;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditCharacterControls.Root;

public partial class ImagePicker : UserControl
{
    private ScriptImageLoader ScriptImageLoader { get; set; }
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private int _selectedImage = -1;

    public event EventHandler<SimpleEventArgs<MutableCharacter>>? OnDelete;

    private int SelectedImage
    {
        get => _selectedImage;
        set
        {
            if (value >= _imageCount)
            {
                _selectedImage = 0;
            }
            else if (value < 0)
            {
                _selectedImage = _imageCount - 1;
            }
            else
            {
                _selectedImage = value;
            }

        }
    }
    private int _imageCount = -1;

    public ImagePicker()
    {
        InitializeComponent();
        ScriptImageLoader = ScriptImageLoader.Default;
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (SelectedImage == -1)
            {
                return;
            }

            --SelectedImage;
            TaskManager.ScheduleAsyncTask(async () =>
            {
                LoadedImage.Source = await ScriptImageLoader.GetImageAsync(LoadedCharacter, SelectedImage);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void ReplaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedImage == -1)
        {
            return;
        }

        TaskManager.ScheduleAsyncTask(async () =>
        {
            TopLevel top = TopLevel.GetTopLevel(this) ?? throw new NoNullAllowedException();
            IReadOnlyList<IStorageFile> files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    FilePickerFileTypes.ImagePng,
                    FilePickerFileTypes.ImageWebp,
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            IStorageFile file = files.Single();

            bool isSuccess = await ScriptImageLoader.TrySetImageAsync(LoadedCharacter, SelectedImage, file, MagickFormat.Png);

            if (!isSuccess)
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", "Could not load image file")
                    .ShowAsPopupAsync(this);
            }
        });
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedImage == -1)
        {
            return;
        }

        ++SelectedImage;
        TaskManager.ScheduleAsyncTask(async () =>
        {
            LoadedImage.Source = await ScriptImageLoader.GetImageAsync(LoadedCharacter, SelectedImage);
        });
    }

    public void Load(MutableCharacter loadedCharacter, ScriptImageLoader loader, IImage image)
    {
        LoadedCharacter = loadedCharacter;
        ScriptImageLoader = loader;
        LoadedImage.Source = image;
        SetToFirstImage();
        LoadedCharacter.PropertyChanged += LoadedCharacter_PropertyChanged;
        ScriptImageLoader.ReloadImage += ScriptImageLoader_ReloadImage;
    }

    private void SetToFirstImage()
    {
        _selectedImage = 0;
        _imageCount = LoadedCharacter.Team switch
        {
            TeamEnum.Traveller => 3,
            TeamEnum.Townsfolk or TeamEnum.Outsider or TeamEnum.Minion or TeamEnum.Demon => 2,
            _ => 1
        };
    }

    private void LoadedCharacter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedCharacter.Team):
                SetToFirstImage();
                break;
        }
    }

    private void ScriptImageLoader_ReloadImage(object? sender, KeyArgs e)
    {
        if (SelectedImage == -1)
        {
            return;
        }

        if (LoadedCharacter.Image.ElementAtOrDefault(SelectedImage) == e.Key || e.Key is null)
        {
            TaskManager.ScheduleAsyncTask(async () =>
            {
                LoadedImage.Source = await ScriptImageLoader.GetImageAsync(LoadedCharacter, _selectedImage);
            });
        }
    }


    public void Lock()
    {
        ReplaceButton.IsEnabled = false;
    }

    public void Unlock()
    {
        ReplaceButton.IsEnabled = true;
    }

    public void Delete()
    {
        LoadedCharacter.PropertyChanged -= LoadedCharacter_PropertyChanged;
        ScriptImageLoader.ReloadImage -= ScriptImageLoader_ReloadImage;
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (!IsShiftModeEnabled())
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    $"Are you sure you want to delete \"{LoadedCharacter.Name}\"?",
                    ButtonEnum.YesNo).ShowWindowDialogAsync(App.MainWindow);
                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }

            OnDelete?.Invoke(this, new SimpleEventArgs<MutableCharacter>(LoadedCharacter));
        });
    }

    private bool IsShiftModeEnabled() =>
        App.IsKeyDown(Key.LeftShift, Key.RightShift) &&
        DeleteButton.IsPointerOver;
}