
// SPDX-License-Identifier: GPL-3.0-or-later



using System.Text.Json.Serialization;

namespace SnapX.Core.Upload.OAuth;

public class OAuth2Token
{
    public string access_token { get; set; }
    public string? refresh_token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }
    public string scope { get; set; }

    public DateTime ExpireDate { get; set; }

    [JsonIgnore]
    public bool IsExpired
    {
        get
        {
            return ExpireDate == DateTime.MinValue || DateTime.UtcNow > ExpireDate;
        }
    }

    public void UpdateExpireDate()
    {
        ExpireDate = DateTime.UtcNow + TimeSpan.FromSeconds(expires_in - 10);
    }
}
