using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Clockmaker0.Data;
using ImageMagick;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Pikcube.ReadWriteScript.Core.Mutable;

namespace Clockmaker0.Controls.EditMetaControls.Tabs;

/// <inheritdoc />
public partial class CustomBackground : UserControl
{
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;

    /// <inheritdoc />
    public CustomBackground()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the current custom background and initialize the relevant events. May only be called once.
    /// </summary>
    /// <param name="loadedMeta">The meta containing the background</param>
    /// <param name="loader">The image loader for the script</param>
    public void Load(MutableMeta loadedMeta, ScriptImageLoader loader)
    {
        LoadedMeta = loadedMeta;
        Loader = loader;

        LoadedMeta.PropertyChanged += LoadedMeta_PropertyChanged;
        ReplaceButton.Click += ReplaceButton_Click;
        DeleteButton.Click += DeleteButton_Click;

        UpdateBackground();
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadedMeta.Background = null;
    }

    private void ReplaceButton_Click(object? sender, RoutedEventArgs e)
    {
        IStorageProvider? sp = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (sp is null)
        {
            //I have no fucking idea how we got here
            IMsBox<ButtonResult> msg = MessageBoxManager.GetMessageBoxStandard("Storage Provider Error",
                "Something has gone wrong and we are unable to activate the file picker. Please report this problem to the developer");
            TaskManager.ScheduleTask(async () => await msg.ShowAsPopupAsync(this));

            return;
        }

        TaskManager.ScheduleAsyncTask(async () =>
        {
            IReadOnlyList<IStorageFile> files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
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

            IStorageFile file = files[0];

            bool result = await Loader.TrySetImageAsync("script/background.png", file, MagickFormat.Png);
            if (result)
            {
                LoadedMeta.Background = "script/background.png";
            }
            else
            {
                IMsBox<ButtonResult> msg = MessageBoxManager.GetMessageBoxStandard("Storage Provider Error",
                    "Something has gone wrong and we are unable to load your image. Please report this problem to the developer");
            }
        });
    }

    private void LoadedMeta_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(LoadedMeta.Background):
                UpdateBackground();
                break;
        }
    }

    private void UpdateBackground()
    {
        if (LoadedMeta.Background is null)
        {
            BackgroundImage.Source = null;
            ReplaceButton.Content = "Add Custom Background";
            DeleteButton.IsVisible = false;
            DeleteButton.IsEnabled = false;
        }
        else
        {
            ReplaceButton.Content = "Replace Custom Background";
            DeleteButton.IsVisible = true;
            DeleteButton.IsEnabled = true;
            TaskManager.ScheduleAsyncTask(async () =>
            {
                BackgroundImage.Source = await Loader.GetImageAsync(LoadedMeta.Background, "script/background.png");
            });
        }
    }
}