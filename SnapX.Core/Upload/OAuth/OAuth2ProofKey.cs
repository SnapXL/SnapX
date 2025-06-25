
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Security.Cryptography;
using System.Text;

namespace SnapX.Core.Upload.OAuth;

public enum OAuth2ChallengeMethod
{
    Plain, SHA256
}

public class OAuth2ProofKey
{
    public string? CodeVerifier { get; private set; }
    public string? CodeChallenge { get; private set; }
    private OAuth2ChallengeMethod Method;
    public string? ChallengeMethod
    {
        get
        {
            switch (Method)
            {
                case OAuth2ChallengeMethod.Plain: return "plain";
                case OAuth2ChallengeMethod.SHA256: return "S256";
            }
            return "";
        }
    }

    public OAuth2ProofKey(OAuth2ChallengeMethod method)
    {
        Method = method;

        var buffer = RandomNumberGenerator.GetBytes(32);


        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);
        CodeVerifier = CleanBase64(buffer);
        CodeChallenge = CodeVerifier;
        if (Method != OAuth2ChallengeMethod.SHA256) return;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(CodeVerifier));
        CodeChallenge = CleanBase64(hash);
    }

    private string? CleanBase64(byte[] buffer)
    {
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

