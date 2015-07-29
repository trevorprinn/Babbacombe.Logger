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
using System.Text;

namespace Babbacombe.Logger {

    /// <summary>
    /// The base class for sending logs and other info to a website as a zip file.
    /// See WebFiles\index.php in the source for a sample website.
    /// </summary>
    public abstract class WebLogSender : LogSender {

        /// <summary>
        /// Gets the url of the website to send the zip files to.
        /// </summary>
        protected abstract string FaultReportUrl { get; }

        /// <summary>
        /// If there are unsent zip files in the Fault Folder, attempts to send them to the website.
        /// </summary>
        public override void SendUnsentFiles(IEnumerable<FileInfo> zips) {
            if (zips == null || !zips.Any()) return;
            try {
                foreach (var file in zips) {
                    try { LogFile.Log("Trying to send existing fault report " + file.Name); } catch { }
                    using (var s = file.OpenRead()) {
                        if (!SendZip(s)) return;
                    }
                    file.Delete();
                }
                Directory.Delete(FaultFolder);
                LogFile.Log("Sent all existing fault reports");
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Uploads the zip file to the website.
        /// </summary>
        /// <param name="dataStream">The zip stream to send.</param>
        /// <returns>True if sent, False if the attempt fails.</returns>
        protected override bool SendZip(Stream dataStream) {
            try {
                // Note that the file name isn't used at the far end.
                using (var resp = uploadZip(FaultReportUrl, dataStream, "Report.zip")) {
                    if (resp.StatusCode != HttpStatusCode.OK) {
                        LogFile.Log("Attempt to send fault returned " + resp.StatusDescription);
                    }
                    return resp.StatusCode == HttpStatusCode.OK;
                }
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Uploads the stream data to the website as a zip file.
        /// </summary>
        /// <param name="hostSite"></param>
        /// <param name="data"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        private HttpWebResponse uploadZip(string hostSite, Stream data, string filename) {
            var req = (HttpWebRequest)WebRequest.Create(hostSite);
            req.Method = "POST";
            string delim = string.Format("{0}{1}", new string('-', 10), Guid.NewGuid().ToString());
            req.ContentType = "multipart/form-data; boundary=" + delim;
            var shead = new StringBuilder();
            shead.AppendFormat("--{0}\r\n", delim);
            shead.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\";\r\n", filename);
            shead.Append("Content-Type: application/x-zip-compressed\r\n\r\n");
            var header = Encoding.ASCII.GetBytes(shead.ToString());
            var footer = Encoding.ASCII.GetBytes(string.Format("\r\n--{0}--\r\n", delim));
            req.ContentLength = header.Length + data.Length + footer.Length;
            using (var reqStream = req.GetRequestStream()) {
                reqStream.Write(header, 0, header.Length);
                data.CopyTo(reqStream);
                reqStream.Write(footer, 0, footer.Length);
                return (HttpWebResponse)req.GetResponse();
            }
        }


    }
}
