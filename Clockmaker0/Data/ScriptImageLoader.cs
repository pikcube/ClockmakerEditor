using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ImageMagick;
using JetBrains.Annotations;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Data;

/// <summary>
/// Responsible for loading, cacheing, unloading, modifying, and reloading any images that needs to be displayed as part of the loaded script
/// </summary>
public class ScriptImageLoader : IDisposable
{

    private bool _disposed;
    private int _version;

    private static Assembly ThisAssembly { get; } = Assembly.GetAssembly(typeof(ScriptImageLoader)) ?? throw new NoNullAllowedException();

    /// <summary>
    /// Increments every time a new image is set using SetImage. By checking this value over time, you can determine whether or not any changes have been made to the underlying images
    /// </summary>
    public int Version => _version;

    private Func<string, Stream?> GetEntryStream { get; }
    private Func<string, Stream>? GetSetEntryStream { get; }
    private Action? DisposeAction { get; }

    private HttpClient? Client { get; set; }
    private ConcurrentDictionary<string, Bitmap> LoadedImages { get; } = [];
    private static ConcurrentDictionary<string, Bitmap> OfficialImages { get; } = [];
    /// <summary>
    /// Default image loader, always returns null, can't set images. Useful for intitalizing controls prior to data being loaded.
    /// </summary>
    public static ScriptImageLoader Default { get; } = new(_ => null);

    /// <summary>
    /// Raised when something when a loaded image has been changed. Controls that get images from the image loader can subscribe to this event to be notified that they may need to replace their loaded image with a new copy. 
    /// </summary>
    public event EventHandler<KeyArgs>? ReloadImage;

    /// <summary>
    /// Raised after a character has been forked, useful for unlocking controls that are currently in read only mode.
    /// </summary>
    public event EventHandler<ValueChangedArgs<MutableCharacter>>? OnFork;

    /// <summary>
    /// Create a Script Image Loader for a Zip Archive in the Clockmaker Format
    /// </summary>
    /// <param name="clockmakerArchive"></param>
    public ScriptImageLoader(ZipArchive clockmakerArchive)
    {
        DisposeAction = clockmakerArchive.Dispose;
        GetEntryStream = s =>
        {
            ZipArchiveEntry? entry = clockmakerArchive.GetEntry(s);
            return entry?.Open();
        };
        GetSetEntryStream = s =>
        {
            clockmakerArchive.GetEntry(s)?.Delete();
            ZipArchiveEntry entry = clockmakerArchive.CreateEntry(s);
            return entry.Open();
        };

        ReloadImage += (_, _) => { ++_version; };

        _version = 0;
    }

    private ScriptImageLoader(Func<string, Stream?> getEntry, Func<string, Stream>? getSetEntry = null, Action? disposeAction = null)
    {
        GetEntryStream = getEntry;
        GetSetEntryStream = getSetEntry;
        DisposeAction = disposeAction;

        ReloadImage += (_, _) => { ++_version; };

        _version = 0;
    }

    /// <summary>
    /// Fallback to call dispose
    /// </summary>
    ~ScriptImageLoader()
    {
        Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Dispose of all the bitmaps
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        DisposeAction?.Invoke();
        foreach ((string _, Bitmap b) in LoadedImages)
        {
            b.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get the image at the specified key
    /// </summary>
    /// <param name="key">The path to the image</param>
    /// <param name="defaultPath">The fallback path (if we are working on a copy)</param>
    /// <returns>The image or an error image</returns>
    public async Task<Bitmap> GetImageAsync(ReferenceProperty<string> key, string defaultPath)
    {
        if (LoadedImages.TryGetValue(key.Get(), out Bitmap? img))
        {
            return img;
        }

        if (key.Get().StartsWith("http"))
        {
            return await GetBitmapFromUrlAsync(key, defaultPath);
        }

        await using Stream? data = GetEntryStream(key.Get());

        if (data is not null)
        {
            using MagickImage image = new(data);
            return await LoadBitmapFromMagickImageAsync(image, key.Get());
        }

        if (key.Get() == defaultPath)
        {
            return GetDefault(TeamEnum.Special, -1);
        }
        
        key.Set(defaultPath);

        return await GetImageAsync(key, defaultPath);
    }

    private async Task<Bitmap> GetBitmapFromUrlAsync(ReferenceProperty<string> key, string defaultPath)
    {
        try
        {
            Client ??= new HttpClient();
            await using Stream webStream = await Client.GetStreamAsync(key.Get());
            using MagickImage magick = new(webStream);

            if (GetSetEntryStream is null)
            {
                return await LoadBitmapFromMagickImageAsync(magick, defaultPath);
            }

            await using Stream zipStream = GetSetEntryStream.Invoke(defaultPath);
            await magick.WriteAsync(zipStream, format: MagickFormat.Png);

            key.Set(defaultPath);

            return await LoadBitmapFromMagickImageAsync(magick, defaultPath);
        }
        catch (HttpRequestException)
        {
            return GetDefault(TeamEnum.Special, -2);
        }
        catch (UriFormatException)
        {
            return GetDefault(TeamEnum.Special, -3);
        }
        catch (Exception)
        {
            return GetDefault(TeamEnum.Special, -1);
        }
    }

    /// <summary>
    /// Get an image from the loader based on its index
    /// </summary>
    /// <param name="c">The character</param>
    /// <param name="index">The index into the image keys list</param>
    /// <returns>The image or a default if it doesn't exist</returns>
    public async Task<Bitmap> GetImageAsync(ICharacter c, int index)
    {
        if (index < 0 || index >= c.Image.Count)
        {
            return GetDefault(c.Team, index);
        }

        string key = c.Image[index];

        if (ScriptParse.IsOfficial(c.Id))
        {
            return await GetOfficialImageAsync(c, index, key);
        }

        if (LoadedImages.TryGetValue(key, out Bitmap? img))
        {
            return img;
        }

        if (key.StartsWith("http"))
        {
            return await GetBitmapFromUrlAsync(c, index, key);
        }

        await using Stream? data = GetEntryStream(key);

        if (data is not null)
        {
            using MagickImage image = new(data);
            return await LoadBitmapFromMagickImageAsync(image, key);
        }

        Bitmap defaultBitmap = GetDefault(c.Team, index);
        LoadedImages.TryAdd(c.Image[index], defaultBitmap);
        if (c is not MutableCharacter mc)
        {
            return defaultBitmap;
        }

        bool b = await TrySetImageAsync(mc, index, s =>
        {
            defaultBitmap.Save(s);
            return Task.CompletedTask;
        }, MagickFormat.Png);
        if (b)
        {
            return await GetImageAsync(mc, index);
        }
        return defaultBitmap;

    }



    /// <summary>
    /// Load an image into the cache without returning the Bitmap itself
    /// </summary>
    /// <param name="c">The image</param>
    /// <param name="index">The index into the keys</param>
    public async Task GetCacheImageAsync(MutableCharacter c, int index)
    {
        if (index < 0 || index > c.Image.Count)
        {
            return;
        }

        string key = c.Image[index];

        if (ScriptParse.IsOfficial(c.Id))
        {
            if (OfficialImages.TryGetValue(key, out Bitmap? _))
            {
                return;
            }

            _ = await GetOfficialImageAsync(c, index, key);
            return;
        }

        if (LoadedImages.TryGetValue(key, out Bitmap? _))
        {
            return;
        }

        if (key.StartsWith("http"))
        {
            _ = await GetBitmapFromUrlAsync(c, index, key);
            return;
        }

        await using Stream? data = GetEntryStream(key);

        if (data is not null)
        {
            using MagickImage image = new(data);
            _ = await LoadBitmapFromMagickImageAsync(image, key);
            return;
        }

        Bitmap defaultBitmap = GetDefault(c.Team, index);
        LoadedImages.TryAdd(c.Image[index], defaultBitmap);
        _ = await TrySetImageAsync(c, index, s =>
        {
            defaultBitmap.Save(s);
            return Task.CompletedTask;
        }, MagickFormat.Png);
    }

    private static async Task<Bitmap> GetOfficialImageAsync(ICharacter c, int index, string key)
    {
        if (OfficialImages.TryGetValue(key, out Bitmap? img))
        {
            return img;
        }

        await using Stream stream = c.GetImageStream(index);
        Bitmap bmp = new(stream);
        OfficialImages.TryAdd(key, bmp);
        return bmp;
    }

    private async Task<Bitmap> GetBitmapFromUrlAsync(ICharacter c, int index, string key)
    {
        try
        {
            Client ??= new HttpClient();
            if (c.Image is IList<string> images)
            {
                images[index] = $"token/{c.Id}/{index}.png";
            }
            await using Stream webStream = await Client.GetStreamAsync(key);
            using MagickImage magick = new(webStream);

            if (GetSetEntryStream is null)
            {
                return await LoadBitmapFromMagickImageAsync(magick, c.Image[index]);
            }

            await using Stream zipStream = GetSetEntryStream.Invoke(c.Image[index]);
            await magick.WriteAsync(zipStream, format: MagickFormat.Png);

            return await LoadBitmapFromMagickImageAsync(magick, c.Image[index]);
        }

        catch (HttpRequestException)
        {
            return GetDefault(TeamEnum.Special, -2);
        }
        catch (UriFormatException)
        {
            return GetDefault(TeamEnum.Special, -3);
        }
        catch (Exception)
        {
            return GetDefault(TeamEnum.Special, -1);
        }
    }

    private static Bitmap GetDefault(TeamEnum? cTeam, int index)
    {
        cTeam ??= TeamEnum.Fabled;

        Stream stream = ThisAssembly.GetManifestResourceStream($"Clockmaker0.default.{(int)cTeam.Value}_{index}.webp")
                        ?? ThisAssembly.GetManifestResourceStream($"Clockmaker0.default.{0}_{-1}.webp")
                        ?? throw new NoNullAllowedException();

        Bitmap bmp = new(stream);

        return bmp;

    }

    /// <summary>
    /// Raise the reload image event for the default icons
    /// </summary>
    public void ReloadDefault()
    {
        ReloadImage?.Invoke(this, new KeyArgs
        {
            Key = null
        });
    }

    /// <summary>
    /// Create a copy of an official character with a mutated ID
    /// </summary>
    /// <param name="c">The character to fork</param>
    /// <param name="script">The script they are on</param>
    /// <param name="newId">The new character id (will append _new if it exists)</param>
    /// <exception cref="InvalidOperationException">Raised if the character is not an Official Character</exception>
    public async Task ForkAsync(MutableCharacter c, MutableBotcScript script, string newId)
    {
        Character oldCharacter = c.ToImmutable();

        if (!ScriptParse.IsOfficial(c.Id))
        {
            throw new InvalidOperationException("Can only fork official characters");
        }

        if (GetSetEntryStream is null)
        {
            throw new InvalidOperationException();
        }

        if (c.Image == null)
        {
            throw new Exception("Official character has no images, this is normally impossible unless Pikcube screwed up his archive file");
        }

        while (script.Characters.Any(character => character.Id == newId))
        {
            newId += "_new";
        }

        for (int n = 0; n < c.Image.Count; ++n)
        {
            c.Image[n] = $"token/{newId}/{n}.png";
        }

        for (int n = 0; n < c.Image.Count; ++n)
        {
            await using Stream s = oldCharacter.GetImageStream(n);
            await TrySetImageAsync(c, n, s.CopyToAsync, MagickFormat.Png);
        }

        script.QueueJinxIdSwap(c.Id, newId);

        c.Id = newId;

        foreach (string key in c.Image)
        {
            ReloadImage?.Invoke(this, new KeyArgs { Key = key });
        }
        OnFork?.Invoke(c, new ValueChangedArgs<MutableCharacter>(c));
    }

    /// <summary>
    /// Set the image in the image loader if the image repo is writable
    /// </summary>
    /// <param name="c">The character</param>
    /// <param name="index">The index to replace</param>
    /// <param name="file">The file</param>
    /// <param name="format">The file format</param>
    /// <returns>True if the write succeeded</returns>
    public async Task<bool> TrySetImageAsync(MutableCharacter c, int index, IStorageFile file, MagickFormat format)
    {
        await using Stream openReadAsync = await file.OpenReadAsync();
        return await TrySetImageAsync(c, index, openReadAsync.CopyToAsync, format);
    }

    /// <summary>
    /// Set the image in the imgae loader if the image repo is writable
    /// </summary>
    /// <param name="c">The character</param>
    /// <param name="index">The index to replace</param>
    /// <param name="copyToAsync">A function that takes a copy target as a parameter and copies the image to the target stream (useful for Bitmat.Save)</param>
    /// <param name="format">The file format</param>
    /// <returns>True if the write succeeded</returns>
    public async Task<bool> TrySetImageAsync(MutableCharacter c, int index, Func<Stream, Task> copyToAsync, MagickFormat format)
    {
        if (GetSetEntryStream is null || index < 0)
        {
            return false;
        }

        while (index >= c.Image.Count)
        {
            await SetDefaultAsync(c, index, GetSetEntryStream);
        }

        return await TrySetImageAsync(c.Image[index], copyToAsync, format);
    }

    /// <summary>
    /// Set the image in the imgae loader if the image repo is writable
    /// </summary>
    /// <param name="path">The path to the image in the loader</param>
    /// <param name="file">The file</param>
    /// <param name="format">The file format</param>
    /// <returns>True if the write succeeded</returns>
    public async Task<bool> TrySetImageAsync(string path, IStorageFile file, MagickFormat format)
    {
        await using Stream openReadAsync = await file.OpenReadAsync();
        return await TrySetImageAsync(path, openReadAsync.CopyToAsync, format);
    }

    /// <summary>
    /// Set the image in the imgae loader if the image repo is writable
    /// </summary>
    /// <param name="path">The path to the image in the loader</param>
    /// <param name="copyToAsync">A function that takes a copy target as a parameter and copies the image to the target stream (useful for Bitmat.Save)</param>
    /// <param name="format">The file format</param>
    /// <returns>True if the write succeeded</returns>
    [UsedImplicitly]
    public async Task<bool> TrySetImageAsync(string path, Func<Stream, Task> copyToAsync, MagickFormat format)
    {
        if (GetSetEntryStream is null)
        {
            return false;
        }

        await using MemoryStream ms = new();
        await copyToAsync(ms);
        ms.Position = 0;
        MagickImage magick = new(ms)
        {
            Format = format
        };
        await using Stream target = GetSetEntryStream(path);
        await magick.WriteAsync(target);

        if (!LoadedImages.TryRemove(path, out Bitmap? value))
        {
            return true;
        }

        ReloadImage?.Invoke(this, new KeyArgs
        {
            Key = path,
        });
        value.Dispose();

        return true;
    }

    private static async Task SetDefaultAsync(MutableCharacter c, int index, Func<string, Stream> getSetEntry)
    {
        int i = c.Image.Count;
        c.Image.Add($"token/{c.Id}/{i}.png");
        await using Stream target = getSetEntry(c.Image[i]);
        GetDefault(c.Team, index).Save(target);
    }

    private async Task<Bitmap> LoadBitmapFromMagickImageAsync(MagickImage image, string key, bool isOfficial = false)
    {
        await using MemoryStream bitmatMs = new();
        await image.WriteAsync(bitmatMs, MagickFormat.Bmp);
        bitmatMs.Position = 0;
        Bitmap bmp = new(bitmatMs);
        if (isOfficial)
        {
            OfficialImages.TryAdd(key, bmp);
        }
        else
        {
            LoadedImages.TryAdd(key, bmp);
        }
        return bmp;
    }

    /// <summary>
    /// Check if the cached version number matches the current version number
    /// </summary>
    /// <param name="imageVersion">The cached version number</param>
    /// <returns>True if the images have been modified, false otherwise</returns>
    public bool IsChanged(int imageVersion) => imageVersion != _version;

    /// <summary>
    /// Force all controls to fetch a new image from the loader
    /// </summary>
    public void ReloadAll()
    {
        ReloadImage?.Invoke(this, new KeyArgs
        {
            Key = null
        });
    }
}

/// <inheritdoc />
public class KeyArgs : EventArgs
{
    /// <summary>
    /// The path to the image in the archive
    /// </summary>
    public required string? Key { get; init; }
}