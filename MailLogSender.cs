#region Licence
/*
The MIT License (MIT)

Copyright (c) 2015 Babbacombe Computers Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Babbacombe.Logger {
    
    /// <summary>
    /// Base class for sending logs and other info via email.
    /// </summary>
    public abstract class MailLogSender : LogSender {

        /// <summary>
        /// Gets the recipients of the email.
        /// </summary>
        protected abstract IEnumerable<string> Recipients { get; }

        /// <summary>
        /// Gets the name of the email server.
        /// </summary>
        protected abstract string SmtpServer { get; }

        /// <summary>
        /// Gets the port for the email server (defaults to 25).
        /// </summary>
        protected virtual int SmtpPort { get { return 25; } }

        /// <summary>
        /// If frue, messages are sent using the default credentials. If false,
        /// the default, messages are sent anonymously unless SmtpUsername/SmtpPassword are defined.
        /// </summary>
        protected virtual bool UseDefaultCredentials { get { return false; } }

        /// <summary>
        /// Gets the username for the email server. Null, the default, if authentication is not required
        /// or the default credentials are being used.
        /// </summary>
        protected virtual string SmtpUsername { get { return null; } }

        /// <summary>
        /// Gets who the message is from.
        /// </summary>
        protected abstract string From { get; }

        /// <summary>
        /// Gets the subject for the email. Defaults to "Error report".
        /// </summary>
        protected virtual string Subject { get { return "Error report"; } }

        /// <summary>
        /// Gets the message body of the email. Defaults to "Log Files are attached".
        /// </summary>
        protected virtual string Body { get { return "Log Files are attached"; } }

        /// <summary>
        /// Gets the password for the email server.
        /// </summary>
        protected virtual string SmtpPassword { get { return null; } }

        /// <summary>
        /// The name to give the report attachment when a single file is sent (not used when
        /// unsent files are re-sent). Defaults to "Report.zip".
        /// </summary>
        protected virtual string AttachmentName { get { return "Report.zip"; } }

        /// <summary>
        /// Gets the Reply To for the email. Defaults to null, which doesn't set it.
        /// </summary>
        protected virtual string ReplyTo { get { return null; } }

        /// <summary>
        /// Sends a zip to the smtp server.
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns></returns>
        protected override bool SendZip(Stream dataStream) {
            try {
                using (var smtp = createClient())
                using (var message = createMessage()) {
                    message.Attachments.Add(new Attachment(dataStream, AttachmentName));

                    smtp.Send(message);
                    return true;
                }
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
                return false;
            }
        }

        private SmtpClient createClient() {
            var smtp = new SmtpClient(SmtpServer, SmtpPort);
            if (UseDefaultCredentials) {
                smtp.UseDefaultCredentials = true;
            } else if (SmtpUsername != null) {
                smtp.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
            }
            return smtp;
        }

        private MailMessage createMessage() {
            var message = new MailMessage();
            message.From = new MailAddress(From);
            message.Subject = Subject;
            message.Body = Body;
            foreach (var rec in Recipients) message.To.Add(new MailAddress(rec));
#if NET35
            if (ReplyTo != null) message.ReplyTo = new MailAddress(ReplyTo);
#else
            if (ReplyTo != null) message.ReplyToList.Add(new MailAddress(ReplyTo));
#endif
            return message;
        }

        /// <summary>
        /// Attempts to resend unsent zip files by email. All of the unsent files are sent
        /// as attachments to a single email.
        /// </summary>
        /// <param name="zips"></param>
        public override void SendUnsentFiles(IEnumerable<FileInfo> zips) {
            if (zips == null || !zips.Any()) return;
            try {
                using (var smtp = createClient())
                using (var message = createMessage()) {
                    foreach (var zip in zips) message.Attachments.Add(new Attachment(zip.FullName));
                    smtp.Send(message);
                }
                foreach (var zip in zips) zip.Delete();
                Directory.Delete(FaultFolder);
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
            }
        }
    }
}
