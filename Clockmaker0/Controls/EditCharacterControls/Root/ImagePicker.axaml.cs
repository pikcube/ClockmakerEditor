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


/// <summary>
/// A control for scrolling through and replacing images
/// </summary>
public partial class ImagePicker : UserControl, IDelete, IOnDelete, ILock
{
    private ScriptImageLoader ScriptImageLoader { get; set; }
    private MutableCharacter LoadedCharacter { get; set; } = MutableCharacter.Default;
    private int _selectedImage = -1;

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <summary>
    /// Load the current image data
    /// </summary>
    /// <param name="loadedCharacter">The character to load</param>
    /// <param name="loader">The image loader</param>
    /// <param name="image">The image to display, null if one needs to be fetched from the image loader</param>
    public void Load(MutableCharacter loadedCharacter, ScriptImageLoader loader, IImage? image = null)
    {
        LoadedCharacter = loadedCharacter;
        ScriptImageLoader = loader;
        TaskManager.ScheduleAsyncTask(async () =>
        {
            image ??= await loader.GetImageAsync(loadedCharacter, 0);

            LoadedImage.Source = image;
            LoadedCharacter.PropertyChanged += LoadedCharacter_PropertyChanged;
            ScriptImageLoader.ReloadImage += ScriptImageLoader_ReloadImage;
            SetToFirstImage();
        });

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


    /// <inheritdoc />
    public void Lock()
    {
        ReplaceButton.IsEnabled = false;
    }

    /// <inheritdoc />
    public void Unlock()
    {
        ReplaceButton.IsEnabled = true;
    }

    /// <inheritdoc />
    public void Delete()
    {
        LoadedCharacter.PropertyChanged -= LoadedCharacter_PropertyChanged;
        ScriptImageLoader.ReloadImage -= ScriptImageLoader_ReloadImage;
    }

    private void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            if (TopLevel.GetTopLevel(this) is not Window top)
            {
                return;
            }
            if (!IsShiftModeEnabled())
            {
                ButtonResult result = await MessageBoxManager.GetMessageBoxStandard("Confirm Delete",
                    $"Are you sure you want to delete \"{LoadedCharacter.Name}\"?",
                    ButtonEnum.YesNo).ShowWindowDialogAsync(top);
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