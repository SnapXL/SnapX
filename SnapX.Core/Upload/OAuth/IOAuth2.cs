// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.OAuth;

public interface IOAuth2 : IOAuth2Basic
{
    bool RefreshAccessToken();

    bool CheckAuthorization();
}

