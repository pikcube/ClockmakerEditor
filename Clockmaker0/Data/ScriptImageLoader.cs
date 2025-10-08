﻿using System;
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

public class ScriptImageLoader : IDisposable
{

    private bool _disposed;
    private int _version;

    private static Assembly ThisAssembly { get; } = Assembly.GetAssembly(typeof(ScriptImageLoader)) ?? throw new NoNullAllowedException();

    public int Version => _version;

    private Func<string, Stream?> GetEntryStream { get; }
    private Func<string, Stream>? GetSetEntryStream { get; }
    private Action? DisposeAction { get; }

    private HttpClient? Client { get; set; }
    private ConcurrentDictionary<string, Bitmap> LoadedImages { get; } = [];
    private static ConcurrentDictionary<string, Bitmap> OfficialImages { get; } = [];
    private static ConcurrentDictionary<(TeamEnum, int), Bitmap> DefaultImages { get; } = [];
    public static ScriptImageLoader Default { get; } = new(_ => null);

    public event EventHandler<KeyArgs>? ReloadImage;
    public event EventHandler<ValueChangedArgs<string>>? BeforeFork;
    public event EventHandler<ValueChangedArgs<MutableCharacter>>? OnFork;

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

    public ScriptImageLoader(Func<string, Stream?> getEntry, Func<string, Stream>? getSetEntry = null, Action? disposeAction = null)
    {
        GetEntryStream = getEntry;
        GetSetEntryStream = getSetEntry;
        DisposeAction = disposeAction;

        ReloadImage += (_, _) => { ++_version; };

        _version = 0;
    }

    ~ScriptImageLoader()
    {
        Dispose();
        _disposed = true;
    }

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

    public async Task<Bitmap> GetImageAsync(string key, string defaultPath)
    {
        if (LoadedImages.TryGetValue(key, out Bitmap? img))
        {
            return img;
        }

        if (key.StartsWith("http"))
        {
            return await GetBitmapFromUrlAsync(key, defaultPath);
        }

        await using Stream? data = GetEntryStream(key);

        if (data is not null)
        {
            using MagickImage image = new(data);
            return await LoadBitmapFromMagickImageAsync(image, key);
        }

        if (key == defaultPath)
        {
            return GetDefault(TeamEnum.Special, -1);
        }

        return await GetImageAsync(defaultPath, defaultPath);
    }

    private async Task<Bitmap> GetBitmapFromUrlAsync(string key, string defaultPath)
    {
        try
        {
            Client ??= new HttpClient();
            await using Stream webStream = await Client.GetStreamAsync(key);
            using MagickImage magick = new(webStream);

            if (GetSetEntryStream is null)
            {
                return await LoadBitmapFromMagickImageAsync(magick, defaultPath);
            }

            await using Stream zipStream = GetSetEntryStream.Invoke(defaultPath);
            await magick.WriteAsync(zipStream, format: MagickFormat.Png);

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

    [UsedImplicitly]
    public static Bitmap GetDefault(TeamEnum? cTeam, int index)
    {
        cTeam ??= TeamEnum.Fabled;

        if (DefaultImages.TryGetValue((cTeam.Value, index), out Bitmap? value))
        {
            return value;
        }

        using Stream stream = ThisAssembly.GetManifestResourceStream($"Clockmaker0.default.{(int)cTeam.Value}_{index}.webp")
                        ?? ThisAssembly.GetManifestResourceStream($"Clockmaker0.default.{0}_{-1}.webp")
                        ?? throw new NoNullAllowedException();

        Bitmap bmp = new(stream);

        DefaultImages.TryAdd((cTeam.Value, index), bmp);
        return bmp;

    }

    public void ReloadDefault()
    {
        ReloadImage?.Invoke(this, new KeyArgs
        {
            Key = null
        });
    }

    public async Task ForkAsync(MutableCharacter c, MutableBotcScript script, string newId)
    {
        BeforeFork?.Invoke(c, new ValueChangedArgs<string>(c.Id));
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
            throw new NoNullAllowedException();
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

    public async Task<bool> TrySetImageAsync(MutableCharacter c, int index, IStorageFile file, MagickFormat format)
    {
        await using Stream openReadAsync = await file.OpenReadAsync();
        return await TrySetImageAsync(c, index, openReadAsync.CopyToAsync, format);
    }

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

    public async Task<bool> TrySetImageAsync(string path, IStorageFile file, MagickFormat format)
    {
        await using Stream openReadAsync = await file.OpenReadAsync();
        return await TrySetImageAsync(path, openReadAsync.CopyToAsync, format);
    }

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

    public bool IsChanged(int imageVersion) => imageVersion != _version;

    public void ReloadAll()
    {
        ReloadImage?.Invoke(this, new KeyArgs
        {
            Key = null
        });
    }
}

public class KeyArgs : EventArgs
{
    public required string? Key { get; init; }
}