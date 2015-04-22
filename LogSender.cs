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
using System.Text;

using ICSharpCode.SharpZipLib.Zip;

namespace Babbacombe.Logger {

    /// <summary>
    /// The base class for sending logs and other info to a website or by email as a zip file.
    /// </summary>
    public abstract class LogSender {

        /// <summary>
        /// Gets the folder where the zip files are stored if sending them fails.
        /// </summary>
        protected abstract string FaultFolder { get; }

        /// <summary>
        /// Collates the files to be sent in the zip as a set of calls to
        /// SendStream, SendString, SendFile and SendAssemblyInfo.
        /// </summary>
        /// <param name="outputStream"></param>
        protected abstract void CollateFiles(ZipOutputStream outputStream);

        /// <summary>
        /// Sends a single zip.
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns></returns>
        protected abstract bool SendZip(Stream dataStream);

        /// <summary>
        /// Sends zips that previously failed to send.
        /// </summary>
        /// <param name="zips"></param>
        public abstract void SendUnsentFiles(IEnumerable<FileInfo> zips);

        /// <summary>
        /// Gets a list of files that have previously failed to send.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<FileInfo> GetUnsentFiles() {
            var dir = new DirectoryInfo(FaultFolder);
            if (!dir.Exists) return new FileInfo[0];
            return dir.GetFiles();
        }

        /// <summary>
        /// Gathers the required info into a zip file and attempts to send it, 
        /// saving it to FaultFolder if the attempt fails.
        /// </summary>
        /// <returns>True if successful</returns>
        public bool Send() {
            using (var datas = GetData()) {
                if (!SendZip(datas)) {
                    SaveInfoToFile(datas);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// True if there are unsent zip files in the FaultFolder.
        /// </summary>
        public bool HasUnsentFiles {
            get { return GetUnsentFiles().Any(); }
        }

        /// <summary>
        /// Collates the data into a zip stream and returns it as a memory stream.
        /// </summary>
        /// <returns></returns>
        protected Stream GetData() {
            MemoryStream mStream = new MemoryStream();
            using (ZipOutputStream zStream = new ZipOutputStream(mStream)) {
                zStream.IsStreamOwner = false;
                CollateFiles(zStream);
            }
            mStream.Seek(0, SeekOrigin.Begin);
            return mStream;
        }

        /// <summary>
        /// Saves the zip stream to a file in FaultFolder.
        /// </summary>
        /// <param name="dataStream"></param>
        protected void SaveInfoToFile(Stream dataStream) {
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
        /// <param name="zs">The zip stream passed into CollateFiles.</param>
        /// <param name="filename">The name to give this file in the zip file.</param>
        /// <param name="stream">The stream containing the data to write into the file in the zip file.</param>
        protected void SendStream(ZipOutputStream zs, string filename, Stream stream) {
            ZipEntry entry = new ZipEntry(filename);
            entry.DateTime = DateTime.UtcNow;
            zs.PutNextEntry(entry);
            stream.CopyTo(zs);
        }

        /// <summary>
        /// Copies the data into a new file in the zip. Call from CollateFiles.
        /// </summary>
        /// <param name="zs">The zip stream passed into CollateFiles.</param>
        /// <param name="filename">The name to give this file in the zip file.</param>
        /// <param name="data">The data to write into the file in the zip file.</param>
        protected void SendString(ZipOutputStream zs, string filename, string data) {
#if NET35
            if (string.IsNullOrEmpty(data)) return;
#else
            if (string.IsNullOrWhiteSpace(data)) return;
#endif
            ZipEntry entry = new ZipEntry(filename);
            entry.DateTime = DateTime.UtcNow;
            zs.PutNextEntry(entry);
            byte[] bdata = Encoding.UTF8.GetBytes(data);
            zs.Write(bdata, 0, bdata.Length);
        }

        /// <summary>
        /// Copies the file contents into a new file in the zip. Call from CollateFiles.
        /// </summary>
        /// <param name="zs">The zip stream passed into CollateFiles.</param>
        /// <param name="filename">The file to include in the zip file.</param>
        /// <param name="zipfilename">
        /// The name to use for the file in the zip file. Defaults to null, which uses the
        /// file name from the filename parameter.
        /// </param>
        protected void SendFile(ZipOutputStream zs, string filename, string zipfilename = null) {
            if (!File.Exists(filename)) return;
            if (zipfilename == null) zipfilename = Path.GetFileName(filename);
            ZipEntry entry = new ZipEntry(zipfilename);
            entry.DateTime = File.GetLastWriteTimeUtc(filename);
            zs.PutNextEntry(entry);
            using (var s = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                s.CopyTo(zs);
            }
        }

        /// <summary>
        /// Copies various OS details and details of assemblies into a new file in the zip.
        /// Call from CollateFiles.
        /// </summary>
        /// <param name="zs">The zip stream passed into CollateFiles.</param>
        /// <param name="filename">The name to give this file in the zip. Defaults to "Assemblies.txt".</param>
        protected void SendAssemblyInfo(ZipOutputStream zs, string filename = "Assemblies.txt") {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("OS Version: {0}", Environment.OSVersion.ToString());
            s.AppendLine();
#if !NET35
            s.AppendFormat("64 Bit: {0}", Environment.Is64BitOperatingSystem ? "Y" : "N");
            s.AppendLine();
#endif
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
    }
}
