using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            foreach (var file in zips) {
                try { LogFile.Log("Trying to send existing fault report " + file.Name); } catch { }
                using (var s = file.OpenRead()) {
                    if (!SendZip(s)) return;
                }
                file.Delete();
            }
            Directory.Delete(FaultFolder);
            LogFile.Log("Sent all existing fault reports");
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
            shead.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", filename);
            shead.Append("Content-Type: application/x-zip-compressed\r\n\r\n");
            var header = Encoding.UTF8.GetBytes(shead.ToString());
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
