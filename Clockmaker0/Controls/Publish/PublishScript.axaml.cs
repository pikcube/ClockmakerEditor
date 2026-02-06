using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Clockmaker0.Data;
using JetBrains.Annotations;
using MsBox.Avalonia;
using Newtonsoft.Json;
using Octokit;
using Pikcube.ReadWriteScript.Core;
using Pikcube.ReadWriteScript.Core.Mutable;
using Pikcube.ReadWriteScript.Offline;

namespace Clockmaker0.Controls.Publish;

/// <inheritdoc />
public partial class PublishScript : Window
{
    private const string EnableSync = "<!--Clockmaker Sync Enabled: Do Not Remove This Comment-->\n";
    private const string NewRepository = "*New Repository*";

    private BotcScript Script { get; init; }
    private ZipArchive ClockFile { get; init; }
    private GitHubClient Client { get; set; } = null!;
    private List<Repository> LoadedRepositories { get; } = [];
    private Repository? SelectedRepository { get; set; }
    private IStorageFile? SelectedFile { get; set; }
    private IStorageFile? Path { get; }

    private string DefaultRepoName { get; set; }

    /// <inheritdoc />
    [UsedImplicitly]
    public PublishScript()
    {
        InitializeComponent();
        DefaultRepoName = "";
        Script = BotcScript.Default;
        ClockFile = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create);
    }

    /// <inheritdoc />
    public PublishScript(BotcScript script, ZipArchive clockFile, IStorageFile? path) : this()
    {
        Script = script;
        ClockFile = clockFile;
        Path = path;

        UploadProgressBar.IsVisible = false;

        Loaded += PublishScript_Loaded;
        BrowseButton.Click += BrowseButton_Click;
        PublishButton.Click += PublishButton_Click;
    }

    private void PublishButton_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectedRepository is null)
        {
            return;
        }

        if (SelectedFile is null)
        {
            return;
        }


        TaskManager.ScheduleAsyncTask(async () =>
        {
            GitHubClient client = Client;
            RepositoryComboBox.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            SavePathTextBox.IsEnabled = false;
            UploadProgressBar.IsVisible = true;
            UploadProgressBar.IsIndeterminate = true;

            if (SelectedRepository.Id == -1)
            {
                SelectedRepository = await GetNewRepoAsync(client, DefaultRepoName);

                while (SelectedRepository is null)
                {
                    SelectedRepository = await GetNewRepoAsync(client, DefaultRepoName + new Random().Next(1000));
                }
            }


            List<RepositoryContent> fileContents = await GetFileContentsAsync(client, SelectedRepository);

            if (fileContents.Count == 0)
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", "An internal error has occcured validating the repo, please contact the developer").ShowAsync();
                return;
            }

            await PublishAsync(Script, ClockFile, SelectedFile, client, SelectedRepository, fileContents);
        });
    }

    private void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        TaskManager.ScheduleAsyncTask(async () =>
        {
            IStorageProvider? sp = GetTopLevel(this)?.StorageProvider;
            if (sp == null)
            {
                //fail
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", "Can't open file picker, contact the developer")
                    .ShowAsPopupAsync(this);
                return;
            }

            IStorageFile? file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = "*.json",
                FileTypeChoices =
                [
                    new FilePickerFileType("Published Json")
                    {
                        Patterns = ["*.json"],
                        AppleUniformTypeIdentifiers = ["public.json"],
                        MimeTypes = ["Application/json"]
                    }
                ],
                ShowOverwritePrompt = true,
                SuggestedFileName = Script.Meta.Name,
                SuggestedStartLocation = await (Path?.GetParentAsync() ?? Task.FromResult<IStorageFolder?>(null)),
                Title = "Save json to disk"

            });

            if (file is null)
            {
                return;
            }

            SelectedFile = file;
            SavePathTextBox.Text = file.Path.AbsolutePath;
            TryEnablePublish();
        });
    }

    private void PublishScript_Loaded(object? sender, RoutedEventArgs e)
    {
        DefaultRepoName = "Clockmaker.Script." + Script.Meta.Name.Replace(" ", "");
        TaskManager.ScheduleAsyncTask(async () =>
        {
            RepositoryComboBox.IsEnabled = false;
            PublishButton.IsEnabled = false;
            RepositoryComboBox.ItemsSource = (string[])["Loading..."];
            RepositoryComboBox.SelectedIndex = 0;
            Client = await CreateClientAsync();
            IAsyncEnumerable<Repository> validRepos = GetValidReposAsync();
            List<string> repoNames = [];
            await foreach (Repository repo in validRepos)
            {
                repoNames.Add(repo.Name);
                LoadedRepositories.Add(repo);
            }

            repoNames.Sort();
            repoNames.Add(NewRepository);
            RepositoryComboBox.ItemsSource = repoNames;
            int index = repoNames.IndexOf(DefaultRepoName);
            RepositoryComboBox.IsEnabled = true;
            RepositoryComboBox.SelectionChanged += RepositoryComboBox_SelectionChanged;
            RepositoryComboBox.SelectedIndex = index == -1 ? repoNames.IndexOf(NewRepository) : index;
            TryEnablePublish();
        });
    }

    private void RepositoryComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RepositoryComboBox.SelectedIndex == -1)
        {
            SelectedRepository = null;
        }

        if (RepositoryComboBox.SelectedItem?.ToString() == NewRepository)
        {
            SelectedRepository = new Repository(-1);
        }
        else
        {
            SelectedRepository =
                LoadedRepositories.SingleOrDefault(r => r.Name == RepositoryComboBox.SelectedItem?.ToString());
        }
        TryEnablePublish();
    }

    private void TryEnablePublish()
    {
        PublishButton.IsEnabled = SelectedRepository is not null && SelectedFile is not null;
    }

    private async IAsyncEnumerable<Repository> GetValidReposAsync()
    {
        IReadOnlyList<Repository> repos = await Client.Repository.GetAllForCurrent(new ApiOptions { PageSize = 100 });

        foreach (Repository repo in repos)
        {
            Readme? readme = await SafelyGetReadMeAsync(Client, repo);
            if (readme is null)
            {
                continue;
            }
            if (readme.Content.StartsWith(EnableSync))
            {
                yield return repo;
            }
        }
    }

    private static async Task<Readme?> SafelyGetReadMeAsync(GitHubClient client, Repository repo)
    {
        try
        {
            return await client.Repository.Content.GetReadme(repo.Id);
        }
        catch (NotFoundException)
        {
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    private async Task PublishAsync(BotcScript script, ZipArchive clockmakerFile, IStorageFile saveFile,
        GitHubClient client, Repository repo, List<RepositoryContent> fileContents)
    {
        MutableBotcScript publishScript = script.ToMutable();

        UploadProgressBar.IsIndeterminate = false;
        UploadProgressBar.Minimum = 0;
        UploadProgressBar.Maximum = GetMaximum();

        double GetMaximum()
        {
            double count = 2;
            if (!string.IsNullOrEmpty(publishScript.Meta.Logo))
            {
                ++count;
            }
            if (!string.IsNullOrEmpty(publishScript.Meta.Background))
            {
                ++count;
            }
            return publishScript.Characters.Where(character => !ScriptParse.IsOfficial(character.Id))
                .Aggregate(count, (current, character) => current + character.Image.Select(clockmakerFile.GetEntry).OfType<ZipArchiveEntry>().Count());
        }

        await UploadLogo();
        await UploadBackground();

        foreach (MutableCharacter character in publishScript.Characters.Where(character => !ScriptParse.IsOfficial(character.Id)))
        {
            for (int index = 0; index < character.Image.Count; index++)
            {
                ZipArchiveEntry? image = clockmakerFile.GetEntry(character.Image[index]);
                if (image is null)
                {
                    continue;
                }

                await using Stream s = await image.OpenAsync();

                RepositoryContentChangeSet? result = await client.CreateOrUpdateContentAsync(fileContents, repo.Id, character.Image[index], await s.ToBase64Async());
                UploadProgressBar.Value++;


                if (result is null)
                {
                    continue;
                }

                character.Image[index] = result.Content.DownloadUrl;
            }
        }

        publishScript.Save();

        string output = ScriptParse.SerializeScript(publishScript, Formatting.Indented);
        await client.CreateOrUpdateContentAsync(fileContents, repo.Id, "script.json", output, true);
        UploadProgressBar.Value++;

        await using Stream save = await saveFile.OpenWriteAsync();
        await using StreamWriter writer = new(save);
        await writer.WriteAsync(output);

        await using MemoryStream zipms = new();
        await using ZipArchive newArchive = new(zipms, ZipArchiveMode.Create);
        foreach (ZipArchiveEntry entry in clockmakerFile.Entries)
        {
            ZipArchiveEntry newEntry = newArchive.CreateEntry(entry.FullName);
            await using Stream newStream = await newEntry.OpenAsync();
            await using Stream oldStream = await entry.OpenAsync();
            await oldStream.CopyToAsync(newStream);
        }

        await client.Repository.Content.UpdateFile(repo.Id, "README.MD",
            new UpdateFileRequest("Updating Readme", MakeReadMe(script), fileContents.Single(c => c.Path == "README.MD").Sha));
        UploadProgressBar.Value++;

        await Launcher.LaunchUriAsync(new Uri(repo.HtmlUrl));
        return;

        async Task UploadLogo()
        {
            if (string.IsNullOrEmpty(publishScript.Meta.Logo))
            {
                return;
            }

            ZipArchiveEntry? image = clockmakerFile.GetEntry(publishScript.Meta.Logo);
            if (image is null)
            {
                return;
            }

            await using Stream s = await image.OpenAsync();

            RepositoryContentChangeSet? result = await client.CreateOrUpdateContentAsync(fileContents, repo.Id,
                publishScript.Meta.Logo, await s.ToBase64Async());
            UploadProgressBar.Value++;

            if (result is null)
            {
                return;
            }

            publishScript.Meta.Logo = result.Content.DownloadUrl;
        }

        async Task UploadBackground()
        {
            if (string.IsNullOrEmpty(publishScript.Meta.Background))
            {
                return;
            }

            ZipArchiveEntry? image = clockmakerFile.GetEntry(publishScript.Meta.Background);
            if (image is null)
            {
                return;
            }


            await using Stream s = await image.OpenAsync();

            RepositoryContentChangeSet? result = await client.CreateOrUpdateContentAsync(fileContents, repo.Id,
                publishScript.Meta.Background, await s.ToBase64Async());
            UploadProgressBar.Value++;

            if (result is null)
            {
                return;
            }

            publishScript.Meta.Background = result.Content.DownloadUrl;
        }
    }

    private static string MakeReadMe(BotcScript script)
    {
        return
            $"{EnableSync}# {script.Meta.Name}\n## Characters\n{string.Join("\n", script.Characters.Select(c => $"* {c.Name}: {c.Ability}"))}";
    }

    private static async Task<List<RepositoryContent>> GetFileContentsAsync(GitHubClient client, Repository repo)
    {
        IReadOnlyList<RepositoryContent> contents = await client.Repository.Content.GetAllContents(repo.Id);
        return await PopulateContentsAsync(client, repo.Id, contents);
    }

    private static async Task<Repository?> GetNewRepoAsync(GitHubClient client, string repoName)
    {
        IReadOnlyList<Repository>? repos = await client.Repository.GetAllForCurrent();
        Repository? repo = repos.SingleOrDefault(r => r.Name == repoName);
        if (repo is not null)
        {
            return null;
        }

        NewRepository repository = new(repoName)
        {
            Visibility = RepositoryVisibility.Public
        };

        repo = await client.Repository.Create(repository);
        await client.Repository.Content.CreateFile(repo.Id, "README.MD",
            new CreateFileRequest("Added Readme", EnableSync));

        return repo;
    }

    private static async Task<List<RepositoryContent>> PopulateContentsAsync(GitHubClient client, long repoId, IReadOnlyList<RepositoryContent> contents)
    {
        List<RepositoryContent> fileContents = [];
        foreach (RepositoryContent content in contents)
        {
            switch (content.Type.Value)
            {
                case ContentType.File:
                    fileContents.Add(content);
                    continue;
                case ContentType.Dir:
                    fileContents.AddRange(await PopulateContentsAsync(client, repoId, await client.Repository.Content.GetAllContents(repoId, content.Path)));
                    continue;
                case ContentType.Symlink:
                case ContentType.Submodule:
                default:
                    //invalid, ignore
                    continue;
            }
        }
        return fileContents;
    }

    private async Task<GitHubClient> CreateClientAsync()
    {
        while (true)
        {
            GitHubClient client = new(new ProductHeaderValue("pikcube.clockmaker"));
            OauthToken token = await GetTokenAsync(client);
            client.Credentials = new Credentials(token.AccessToken);
            try
            {
                _ = await client.Repository.Get(1);
            }
            catch (ForbiddenException)
            {
                TokenStore.DeleteTokenData();
                continue;
            }
            catch (AuthorizationException)
            {
                TokenStore.DeleteTokenData();
                continue;
            }
            TokenStore.SaveTokenData(new TokenData(token));
            return client;
        }
    }

    private async Task<OauthToken> GetTokenAsync(GitHubClient client)
    {
        if (TokenStore.TryGetTokenData(out TokenData token) && token.TokenType is not null && token.AccessToken is not null)
        {
            return new OauthToken(token.TokenType, token.AccessToken, token.ExpiresIn, token.RefreshToken, token.RefreshTokenExpiresIn, token.Scope, token.Error, token.ErrorDescription, token.ErrorUri);

        }


        OauthDeviceFlowRequest request = new(Secret.ClientId)
        {
            Scopes = { "repo" }
        };


        OauthDeviceFlowResponse? a = await client.Oauth.InitiateDeviceFlow(request);
        LogInWithGitHub liwg = new();
        liwg.Load(a.UserCode, a.VerificationUri, Launcher);
        PopOutWindow pow = new(liwg, "Log in With Github");
        pow.Show(this);
        OauthToken t = await client.Oauth.CreateAccessTokenForDeviceFlow(Secret.ClientId, a);
        pow.Close();
        return t;
    }
}