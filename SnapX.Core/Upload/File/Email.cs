
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Net.Mail;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;

public class EmailFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Email;
    public override bool CheckConfig(UploadersConfig config)
    {
        return !string.IsNullOrEmpty(config.EmailSmtpServer) && config.EmailSmtpPort > 0 && !string.IsNullOrEmpty(config.EmailFrom) && !string.IsNullOrEmpty(config.EmailPassword);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        if (config.EmailAutomaticSend && !string.IsNullOrEmpty(config.EmailAutomaticSendTo))
        {
            return new Email()
            {
                SmtpServer = config.EmailSmtpServer,
                SmtpPort = config.EmailSmtpPort,
                FromEmail = config.EmailFrom,
                Password = config.EmailPassword,
                ToEmail = config.EmailAutomaticSendTo,
                Subject = config.EmailDefaultSubject,
                Body = config.EmailDefaultBody
            };
        }
        else
        {
            // using (EmailForm emailForm = new EmailForm(config.EmailRememberLastTo ? config.EmailLastTo : "", config.EmailDefaultSubject, config.EmailDefaultBody))
            // {
            //     if (emailForm.ShowDialog() == DialogResult.OK)
            //     {
            //         if (config.EmailRememberLastTo)
            //         {
            //             config.EmailLastTo = emailForm.ToEmail;
            //         }
            //
            //         return new Email()
            //         {
            //             SmtpServer = config.EmailSmtpServer,
            //             SmtpPort = config.EmailSmtpPort,
            //             FromEmail = config.EmailFrom,
            //             Password = config.EmailPassword,
            //             ToEmail = emailForm.ToEmail,
            //             Subject = emailForm.Subject,
            //             Body = emailForm.Body
            //         };
            //     }
            //     else
            //     {
            //         taskInfo.StopRequested = true;
            //     }
            // }
        }

        return null;
    }
}

public class Email : FileUploader
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; }
    public string Password { get; set; }

    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string? Body { get; set; }

    public void Send()
    {
        Send(ToEmail, Subject, Body);
    }

    public void Send(string toEmail, string subject, string? body)
    {
        Send(toEmail, subject, body, null, null);
    }

    public void Send(string toEmail, string subject, string? body, Stream stream, string? fileName)
    {
        using (SmtpClient smtp = new SmtpClient()
        {
            Host = SmtpServer,
            Port = SmtpPort,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(FromEmail, Password)
        })
        {
            using (MailMessage message = new MailMessage(FromEmail, toEmail))
            {
                message.Subject = subject;
                message.Body = body;

                if (stream != null)
                {
                    Attachment attachment = new Attachment(stream, fileName);
                    message.Attachments.Add(attachment);
                }

                smtp.Send(message);
            }
        }
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        Send(ToEmail, Subject, Body, stream, fileName);
        return new UploadResult { IsURLExpected = false };
    }
}
