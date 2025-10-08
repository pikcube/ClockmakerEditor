using Clockmaker0.Data;
using NeoSmart.SecureStore;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml.Linq;
using Octokit;

namespace Clockmaker0;

public static class TokenStore
{
    public static void SaveTokenData(TokenData data)
    {
        using SecretsManager sm = LoadOrCreate(out string path);
        sm.Set("token", JsonConvert.SerializeObject(data));
        sm.SaveStore(path);
    }


    public static void DeleteTokenData()
    {
        using SecretsManager sm = LoadOrCreate(out string path);
        sm.Delete("token");
        sm.SaveStore(path);
    }

    public static bool TryGetTokenData(out TokenData data)
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

    public static void RotateSecrets()
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
