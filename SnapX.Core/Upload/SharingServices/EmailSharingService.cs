// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.File;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.SharingServices;

public class EmailSharingService : URLSharingService
{
    public override URLSharingServices EnumValue => URLSharingServices.Email;

    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.EmailSmtpServer) && config.EmailSmtpPort > 0 && !string.IsNullOrEmpty(config.EmailFrom) && !string.IsNullOrEmpty(config.EmailPassword);
    }

    public override URLSharer CreateSharer(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new EmailSharer(config);
    }
}

public sealed class EmailSharer : URLSharer
{
    private UploadersConfig config;

    public EmailSharer(UploadersConfig config)
    {
        this.config = config;
    }

    public override UploadResult ShareURL(string? url)
    {
        var result = new UploadResult { URL = url, IsURLExpected = false };

        if (config.EmailAutomaticSend && !string.IsNullOrEmpty(config.EmailAutomaticSendTo))
        {
            var email = new Email()
            {
                SmtpServer = config.EmailSmtpServer,
                SmtpPort = config.EmailSmtpPort,
                FromEmail = config.EmailFrom,
                Password = config.EmailPassword,
                ToEmail = config.EmailAutomaticSendTo,
                Subject = config.EmailDefaultSubject,
                Body = url
            };

            email.Send();
        }
        else
        {
            // TODO: Reimplement Email Service
            // I wonder if anyone even uses this shit 😭😭😭
            // using (EmailForm emailForm = new EmailForm(config.EmailRememberLastTo ? config.EmailLastTo : "", config.EmailDefaultSubject, url))
            // {
            //     if (emailForm.ShowDialog() == DialogResult.OK)
            //     {
            //         if (config.EmailRememberLastTo)
            //         {
            //             config.EmailLastTo = emailForm.ToEmail;
            //         }
            //
            //         Email email = new Email()
            //         {
            //             SmtpServer = config.EmailSmtpServer,
            //             SmtpPort = config.EmailSmtpPort,
            //             FromEmail = config.EmailFrom,
            //             Password = config.EmailPassword,
            //             ToEmail = emailForm.ToEmail,
            //             Subject = emailForm.Subject,
            //             Body = emailForm.Body
            //         };
            //
            //         email.Send();
            //     }
            // }
        }

        URLHelpers.OpenURL("mailto:?body=" + URLHelpers.URLEncode(url));

        return result;
    }

}

