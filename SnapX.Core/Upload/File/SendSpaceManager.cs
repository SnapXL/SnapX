// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public static class SendSpaceManager
{
    public static string? Token;
    public static string? SessionKey;
    public static DateTime LastSessionKey;
    public static AccountType AccountType;
    public static string? Username;
    public static string? Password;
    public static SendSpace.UploadInfo UploadInfo;

    public static UploaderErrorManager PrepareUploadInfo(string? apiKey, string? username = null, string? password = null)
    {
        var sendSpace = new SendSpace(apiKey);

        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                AccountType = AccountType.Anonymous;

                UploadInfo = sendSpace.AnonymousUploadGetInfo();
                if (UploadInfo == null) throw new Exception("UploadInfo is null.");
            }
            else
            {
                AccountType = AccountType.User;
                Username = username;
                Password = password;

                if (string.IsNullOrEmpty(Token))
                {
                    Token = sendSpace.AuthCreateToken();
                    if (string.IsNullOrEmpty(Token)) throw new Exception("Token is null or empty.");
                }
                if (string.IsNullOrEmpty(SessionKey) || (DateTime.Now - LastSessionKey).TotalMinutes > 30)
                {
                    SessionKey = sendSpace.AuthLogin(Token, username, password).SessionKey;
                    if (string.IsNullOrEmpty(SessionKey)) throw new Exception("SessionKey is null or empty.");
                    LastSessionKey = DateTime.Now;
                }
                UploadInfo = sendSpace.UploadGetInfo(SessionKey);
                if (UploadInfo == null) throw new Exception("UploadInfo is null.");
            }
        }
        catch (Exception e)
        {
            if (sendSpace.Errors.Count > 0)
            {
                DebugHelper.WriteException(new Exception(sendSpace.ToErrorString()));
            }
            else
            {
                DebugHelper.WriteException(e);
            }
        }

        return sendSpace.Errors;
    }
}
