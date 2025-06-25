
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.OAuth;

public class OAuth2Info
{
    public string? Client_ID { get; set; }
    public string? Client_Secret { get; set; }
    public OAuth2Token Token { get; set; }
    public OAuth2ProofKey Proof { get; set; }
    public string Email { get; set; }

    public OAuth2Info(string? client_id, string? client_secret)
    {
        Client_ID = client_id;
        Client_Secret = client_secret;
    }

    public static bool CheckOAuth(OAuth2Info oauth)
    {
        return oauth != null && !string.IsNullOrEmpty(oauth.Client_ID) && !string.IsNullOrEmpty(oauth.Client_Secret) &&
            oauth.Token != null && !string.IsNullOrEmpty(oauth.Token.access_token);
    }
}

