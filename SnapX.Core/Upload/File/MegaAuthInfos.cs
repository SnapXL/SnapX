// SPDX-License-Identifier: GPL-3.0-or-later


using CG.Web.MegaApiClient;

namespace SnapX.Core.Upload.File;
public class MegaAuthInfos
{
    public string Email { get; set; }
    public string Hash { get; set; }
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

