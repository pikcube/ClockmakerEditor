using Clockmaker0.Data;
using NeoSmart.SecureStore;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Clockmaker0;

/// <summary>
/// Static class for loading and unloading github tokens from disk
/// </summary>
internal static class TokenStore
{
    /// <summary>
    /// Save the current token data to disk
    /// </summary>
    /// <param name="data">The token data to save</param>
    internal static void SaveTokenData(TokenData data)
    {
        using SecretsManager sm = LoadOrCreate(out string path);
        string serializeObject = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        });
        sm.Set("token", serializeObject);
        sm.SaveStore(path);
    }


    /// <summary>
    /// Delete the token from the token store
    /// </summary>
    internal static void DeleteTokenData()
    {
        using SecretsManager sm = LoadOrCreate(out string path);
        sm.Delete("token");
        sm.SaveStore(path);
    }

    /// <summary>
    /// Try to get the current token data from disk
    /// </summary>
    /// <param name="data">The intitialized token data from disk, or an empty object if it doesn't</param>
    /// <returns>True if the object exists on disk, false otherwise</returns>
    internal static bool TryGetTokenData(out TokenData data)
    {
        data = new TokenData();
        using SecretsManager sm = LoadOrCreate(out _);
        if (!sm.TryGetValue("token", out string? tokenData))
        {
            return false;
        }

        if (tokenData is null)
        {
            return false;
        }

        TokenData? td = JsonConvert.DeserializeObject<TokenData>(tokenData);
        if (td is null)
        {
            return false;
        }

        data = td;
        return true;

    }

    /// <summary>
    /// Generate a new encryption key and write it to disk
    /// </summary>
    internal static void RotateSecrets()
    {
        SecretsManager sm = LoadOrCreate(out string path);
        if (!sm.TryGetValue("token", out string? tokenData))
        {
            return;
        }

        if (tokenData is null)
        {
            return;
        }
        sm.Dispose();
        File.Delete(path);
        using SecretsManager sm2 = LoadOrCreate(out path);
        sm2.Set("token", tokenData);
        sm2.SaveStore(path);
    }

    private static SecretsManager LoadOrCreate(out string path)
    {
        string appdata = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clockmaker", "Github");
        if (!Directory.Exists(appdata))
        {
            Directory.CreateDirectory(appdata);
        }

        path = Path.Join(appdata, "profile.json");
        string keyPath = Path.Join(appdata, "profile");

        if (File.Exists(path) && File.Exists(keyPath))
        {
            try
            {
                SecretsManager value = SecretsManager.LoadStore(path);
                value.LoadKeyFromFile(keyPath);
                return value;
            }
            catch (Exception)
            {
                //goto fail
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(keyPath))
                {
                    File.Delete(keyPath);
                }
            }
        }

        SecretsManager manager = SecretsManager.CreateStore();
        manager.GenerateKey();
        manager.ExportKey(keyPath);
        return manager;
    }
}
