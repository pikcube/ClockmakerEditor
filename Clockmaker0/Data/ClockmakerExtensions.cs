using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Octokit;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Data;

/// <summary>
/// Various Extension Methods Not Otherwise Categorized
/// </summary>
public static class ClockmakerExtensions
{
    /// <summary>
    /// Mutate the loaded character's id, then rename any image files in the clockmaker file.
    /// </summary>
    /// <param name="c">The character to fork</param>
    /// <param name="script">The script containing the character</param>
    /// <param name="loader">The image loader</param>
    /// <param name="newId">The new character id</param>
    public static async Task ForkAsync(this MutableCharacter c, MutableBotcScript script, ScriptImageLoader loader, string newId)
    {
        await loader.ForkAsync(c, script, newId);
    }

    /// <summary>
    /// Convert a BotcScript into a MutableBotcScript
    /// </summary>
    /// <param name="script">The script to convert</param>
    /// <returns></returns>
    public static MutableBotcScript ToMutable(this BotcScript script) => new(script, ScriptParse.Base);

    /// <summary>
    /// A hack to call Focus on controls before they are loaded. Calling Focus on a control will normally fail if the control has not been attacked to the visual tree.
    /// If the control isn't loaded yet, it will wait until it loads to call Focus on it.
    /// </summary>
    /// <param name="control">The control to focus</param>
    /// <param name="navigationMethod">The nagivation method (default to unspecified)</param>
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

    private static async Task<byte[]> ToArrayAsync(this Stream s)
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

    /// <summary>
    /// Convert a data stream into a Base64 Stream
    /// </summary>
    /// <param name="s">The stream to convert</param>
    /// <returns>The data stream encoded in Base64</returns>
    public static async Task<string> ToBase64Async(this Stream s) => Convert.ToBase64String(await s.ToArrayAsync());

    /// <summary>
    /// Helper method to replace an existing file or create a new file if it doesn't exist.
    /// </summary>
    /// <param name="client">The github client</param>
    /// <param name="fileContents">All files in the repo</param>
    /// <param name="repoId">The reop id</param>
    /// <param name="path">The path to upload the file to</param>
    /// <param name="content">The content to upload</param>
    /// <param name="convertContentToBase64">Instructs whether or not the string is already base 64 encoded</param>
    /// <returns></returns>
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