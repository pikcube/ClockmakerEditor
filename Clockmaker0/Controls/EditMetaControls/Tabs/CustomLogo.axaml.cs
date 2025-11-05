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
public partial class CustomLogo : UserControl
{
    private MutableMeta LoadedMeta { get; set; } = MutableMeta.Default;
    private ScriptImageLoader Loader { get; set; } = ScriptImageLoader.Default;

    /// <inheritdoc />
    public CustomLogo()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load the option to edit a custom logo
    /// </summary>
    /// <param name="loadedMeta">The meta object containing the logo</param>
    /// <param name="loader">The image loader</param>
    public void Load(MutableMeta loadedMeta, ScriptImageLoader loader)
    {
        LoadedMeta = loadedMeta;
        Loader = loader;

        LoadedMeta.PropertyChanged += LoadedMeta_PropertyChanged;
        ReplaceButton.Click += ReplaceButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        HideTitleCheckBox.IsCheckedChanged += HideTitleCheckBox_IsCheckedChanged;

        UpdateLogo();
    }

    private void HideTitleCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        LoadedMeta.IsHideTitle = HideTitleCheckBox.IsChecked is false;
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadedMeta.Logo = null;
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

            bool result = await Loader.TrySetImageAsync("script/logo.png", file, MagickFormat.Png);
            if (result)
            {
                LoadedMeta.Logo = "script/logo.png";
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
            case nameof(LoadedMeta.Logo):
            case nameof(LoadedMeta.IsHideTitle):
                UpdateLogo();
                break;
        }
    }

    private void UpdateLogo()
    {
        if (LoadedMeta.Logo is null)
        {
            LogoImage.Source = null;
            ReplaceButton.Content = "Add Logo";
            DeleteButton.IsVisible = false;
            DeleteButton.IsEnabled = false;
            HideTitleCheckBox.IsVisible = false;
            HideTitleCheckBox.IsEnabled = false;
            HideTitleCheckBox.IsChecked = !LoadedMeta.IsHideTitle;
        }
        else
        {
            ReplaceButton.Content = "Replace Logo";
            DeleteButton.IsVisible = true;
            DeleteButton.IsEnabled = true;
            HideTitleCheckBox.IsVisible = true;
            HideTitleCheckBox.IsEnabled = true;
            HideTitleCheckBox.IsChecked = !LoadedMeta.IsHideTitle;
            TaskManager.ScheduleAsyncTask(async () =>
            {
                LogoImage.Source = await Loader.GetImageAsync(LoadedMeta.Logo, "script/logo.png");
            });
        }
    }
}