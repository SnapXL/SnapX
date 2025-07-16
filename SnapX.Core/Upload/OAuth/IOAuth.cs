// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.OAuth;

public interface IOAuth : IOAuthBase
{
    OAuthInfo AuthInfo { get; }
}

