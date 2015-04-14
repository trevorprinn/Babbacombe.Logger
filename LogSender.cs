using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ICSharpCode.SharpZipLib.Zip;

namespace Babbacombe.Logger {

    /// <summary>
    /// The base class for sending logs and other info to a website as a zip file.
    /// See WebFiles\index.php in the source for a sample website.
    /// </summary>
    public abstract class LogSender {

        /// <summary>
        /// Gets the folder where the zip files are stored if sending them to the
        /// website fails.
        /// </summary>
        public abstract string FaultFolder { get; }

        /// <summary>
        /// Gets the url of the website to send the zip files to.
        /// </summary>
        public abstract string FaultReportUrl { get; }

        /// <summary>
        /// Collates the files to be sent in the zip as a set of calls to
        /// SendStream, SendString, SendFile and SendAssemblyInfo.
        /// </summary>
        /// <param name="outputStream"></param>
        protected abstract void CollateFiles(ZipOutputStream outputStream);

        /// <summary>
        /// Gathers the required info into a zip file and attempts to send it to
        /// the website, saving it to FaultFolder if the attempt fails.
        /// </summary>
        /// <returns>True if successful</returns>
        public bool Send() {
            using (var datas = getData()) {
                if (!sendStream(datas)) {
                    saveInfoToFile(datas);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// True if there are unsent zip files in the FaultFolder.
        /// </summary>
        public bool HasUnsentFiles {
            get {
                var dir = new DirectoryInfo(FaultFolder);
                if (!dir.Exists) return false;
                return dir.GetFiles().Any();
            }
        }

        /// <summary>
        /// If there are unsent zip files in the Fault Folder, attempts to send them to the website.
        /// </summary>
        public void SendAnyOldFiles() {
            var dir = new DirectoryInfo(FaultFolder);
            if (!dir.Exists) return;
            foreach (var file in dir.GetFiles()) {
                try { LogFile.Log("Trying to send existing fault report " + file.Name); } catch { }
                using (var s = file.OpenRead()) {
                    if (!sendStream(s)) return;
                }
                file.Delete();
            }
            dir.Delete(true);
            LogFile.Log("Sent all existing fault reports");
        }

        /// <summary>
        /// Uploads the zip file to the website.
        /// </summary>
        /// <param name="inStream">The zip stream to send.</param>
        /// <returns>True if sent, False if the attempt fails.</returns>
        private bool sendStream(Stream inStream) {
            try {
                // Note that the file name isn't used at the far end.
                using (var resp = uploadZip(FaultReportUrl, inStream, "Report.zip")) {
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
        /// Collates the data into a zip stream and returns it as a memory stream.
        /// </summary>
        /// <returns></returns>
        private Stream getData() {
            MemoryStream mStream = new MemoryStream();
            using (ZipOutputStream zStream = new ZipOutputStream(mStream)) {
                CollateFiles(zStream);
            }
            mStream.Seek(0, SeekOrigin.Begin);
            return mStream;
        }

        /// <summary>
        /// Saves the zip stream to a file in FaultFolder.
        /// </summary>
        /// <param name="dataStream"></param>
        private void saveInfoToFile(Stream dataStream) {
            try {
                dataStream.Seek(0, SeekOrigin.Begin);
                if (!Directory.Exists(FaultFolder)) Directory.CreateDirectory(FaultFolder);
                string fname = Path.Combine(FaultFolder, DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss") + ".zip");
                using (FileStream fs = new FileStream(fname, FileMode.CreateNew, FileAccess.Write)) {
                    dataStream.CopyTo(fs, 2048);
                }
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
            }
        }

        /// <summary>
        /// Copies the data in the stream into a new file in the zip.
        /// Call from CollateFiles.
        /// </summary>
        /// <param name="zs"></param>
        /// <param name="filename"></param>
        /// <param name="stream"></param>
        protected void SendStream(ZipOutputStream zs, string filename, Stream stream) {
            ZipEntry entry = new ZipEntry(filename);
            entry.DateTime = DateTime.UtcNow;
            zs.PutNextEntry(entry);
            stream.CopyTo(zs);
        }

        /// <summary>
        /// Copies the data into a new file in the zip. Call from CollateFiles.
        /// </summary>
        /// <param name="zs"></param>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        protected void SendString(ZipOutputStream zs, string filename, string data) {
            if (string.IsNullOrWhiteSpace(data)) return;
            ZipEntry entry = new ZipEntry(filename);
            entry.DateTime = DateTime.UtcNow;
            zs.PutNextEntry(entry);
            byte[] bdata = Encoding.UTF8.GetBytes(data);
            zs.Write(bdata, 0, bdata.Length);
        }

        /// <summary>
        /// Copies the file contents into a new file in the zip. Call from CollateFiles.
        /// </summary>
        /// <param name="zs"></param>
        /// <param name="filename"></param>
        /// <param name="zipfilename"></param>
        protected void SendFile(ZipOutputStream zs, string filename, string zipfilename = null) {
            if (!File.Exists(filename)) return;
            if (zipfilename == null) zipfilename = Path.GetFileName(filename);
            ZipEntry entry = new ZipEntry(zipfilename);
            entry.DateTime = File.GetLastWriteTimeUtc(filename);
            zs.PutNextEntry(entry);
            using (var s = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                s.CopyTo(zs);
            }
        }

        /// <summary>
        /// Copies various OS and details of assemblies into a new file in the zip.
        /// Call from CollateFiles.
        /// </summary>
        /// <param name="zs"></param>
        /// <param name="filename"></param>
        protected void SendAssemblyInfo(ZipOutputStream zs, string filename = "Assemblies.txt") {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("OS Version: {0}", Environment.OSVersion.ToString());
            s.AppendLine();
            s.AppendFormat("64 Bit: {0}", Environment.Is64BitOperatingSystem ? "Y" : "N");
            s.AppendLine();
            s.AppendFormat("CLR Version: {0}", Environment.Version.ToString());
            s.AppendLine(); s.AppendLine();
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies()) {
                s.AppendFormat("Name: {0}", ass.FullName);
                s.AppendLine();
                s.AppendFormat("Location: {0}", ass.Location);
                s.AppendLine();
                if (ass.GlobalAssemblyCache) s.AppendLine("From GAC");
                s.AppendFormat("CLR Version: {0}", ass.ImageRuntimeVersion);
                s.AppendLine(); s.AppendLine();
            }
            SendString(zs, filename, s.ToString());
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
            shead.AppendFormat("Content-Disposition: form-data; name=\"file\"; {0}\r\n", filename);
            shead.Append("Content-Type: application/x-zip-compressed\r\n\r\n");
            var header = Encoding.UTF8.GetBytes(shead.ToString());
            var footer = Encoding.ASCII.GetBytes(string.Format("\r\n--{0}--\r\n", delim));
            var contentLen = header.Length + data.Length + footer.Length;
            using (var reqStream = req.GetRequestStream()) {
                reqStream.Write(header, 0, header.Length);
                data.CopyTo(reqStream);
                reqStream.Write(footer, 0, footer.Length);
                return (HttpWebResponse)req.GetResponse();
            }
        }
    }
}
