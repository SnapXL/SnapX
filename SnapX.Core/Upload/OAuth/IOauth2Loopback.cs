
// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.OAuth;

public interface IOAuth2Loopback : IOAuth2
{
    OAuthUserInfo GetUserInfo();

    string? RedirectURI { get; set; }
    string? State { get; set; }
    string? Scope { get; set; }
}

