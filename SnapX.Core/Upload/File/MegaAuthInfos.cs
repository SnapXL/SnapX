
// SPDX-License-Identifier: GPL-3.0-or-later


using CG.Web.MegaApiClient;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class MegaAuthInfos
{
    [JsonEncrypt]
    [YamlEncrypt]
    public string Email { get; set; }
    [JsonEncrypt]
    [YamlEncrypt]
    public string Hash { get; set; }
    [JsonEncrypt]
    [YamlEncrypt]
    public string PasswordAesKey { get; set; }

    public MegaAuthInfos()
    {
    }

    public MegaAuthInfos(MegaApiClient.AuthInfos authInfos)
    {
        Email = authInfos.Email;
        Hash = authInfos.Hash;
        PasswordAesKey = Convert.ToBase64String(authInfos.PasswordAesKey);
    }

    public MegaApiClient.AuthInfos GetMegaApiClientAuthInfos()
    {
        byte[] passwordAesKey = null;

        if (!string.IsNullOrEmpty(PasswordAesKey))
        {
            passwordAesKey = Convert.FromBase64String(PasswordAesKey);
        }

        return new MegaApiClient.AuthInfos(Email, Hash, passwordAesKey);
    }
}

