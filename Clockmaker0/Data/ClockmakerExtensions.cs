using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Octokit;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Data;

public static class ClockmakerExtensions
{
    public static async Task ForkAsync(this MutableCharacter c, MutableBotcScript script, ScriptImageLoader loader, string newId)
    {
        await loader.ForkAsync(c, script, newId);
    }

    public static MutableBotcScript ToMutable(this BotcScript script) => new(script, ScriptParse.GetOfficialCharacters);

    public static void BufferFocus(this Control control, NavigationMethod navigationMethod = NavigationMethod.Unspecified)
    {
        if (control.IsLoaded)
        {
            control.Focus(navigationMethod);
        }
        else
        {
            control.Loaded += ControlLoaded;
        }

        return;

        void ControlLoaded(object? sender, RoutedEventArgs e)
        {
            control.Loaded -= ControlLoaded;
            control.Focusable = true;
            control.Focus(navigationMethod);
        }
    }

    public static MemoryStream ToStream(this Bitmap bitmap)
    {
        MemoryStream ms = new();
        bitmap.Save(ms);
        return ms;
    }

    public static byte[] ToArray(this Stream s)
    {
        if (s is MemoryStream ms)
        {
            return ms.ToArray();
        }

        long position = s.Position;
        s.Position = 0;
        byte[] bytes = new byte[s.Length];
        s.ReadExactly(bytes, 0, bytes.Length);
        s.Position = position;
        return bytes;
    }

    public static async Task<byte[]> ToArrayAsync(this Stream s)
    {
        if (s is MemoryStream ms)
        {
            return ms.ToArray();
        }

        long position = s.Position;
        s.Position = 0;
        byte[] bytes = new byte[s.Length];
        await s.ReadExactlyAsync(bytes, 0, bytes.Length);
        s.Position = position;
        return bytes;
    }

    public static string ToBase64(this Stream s) => Convert.ToBase64String(s.ToArray());

    public static async Task<string> ToBase64Async(this Stream s) => Convert.ToBase64String(await s.ToArrayAsync());

    public static async Task<RepositoryContentChangeSet?> CreateOrUpdateContentAsync(this GitHubClient client, IEnumerable<RepositoryContent> fileContents, long repoId, string path, string content, bool convertContentToBase64 = false)
    {
        RepositoryContent? file = fileContents.SingleOrDefault(c => c.Path == path);
        if (file is not null)
        {
            UpdateFileRequest request = new("Updating token", content, file.Sha, convertContentToBase64);
            return await client.Repository.Content.UpdateFile(repoId, path, request);
        }
        else
        {
            CreateFileRequest request = new("Uploading token", content, convertContentToBase64);
            return await client.Repository.Content.CreateFile(repoId, path, request);
        }
    }
}