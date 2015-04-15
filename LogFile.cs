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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babbacombe.Logger {
    /// <summary>
    /// Manages writing to a log file. The output stream is kept open as long as the LogFile
    /// object exists. The log file can be read externally while the LogFile object has it open.
    /// All messages written have the current date time prepended. The log file can be made a trace
    /// listener, so that all Trace messages are written to it, and contains a static method for
    /// writing to Trace.
    /// </summary>

    public class LogFile : System.Diagnostics.TraceListener {
        private object _synchLock = new object();
        private StreamWriter _output;
        private string _filename;
        private string _headerFormat;

        private bool _isDaily;
        private DateTime _dailyDate;

        /// <summary>
        /// Whether to flush automatically after writing each line. Defaults to True.
        /// </summary>
        public bool AutoFlush { get; set; }

        /// <summary>
        /// Whether to use UTC or local time when logging. Defaults to True.
        /// </summary>
        public bool UseUtc { get; private set; }

        private Mutex _mutex;

        private string _instanceId;

        /// <summary>
        /// Creates a new log file, or appends to an existing one.
        /// </summary>
        /// <param name="filename"></param>
        public LogFile(string filename, bool useUtc = true, bool useMutex = false, bool autoFlush = true) {
            _filename = filename;
            var fs = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _output = new StreamWriter(fs);
            AutoFlush = autoFlush;
            UseUtc = useUtc;

            if (useMutex) _mutex = new Mutex(false, @"Global\BabLogger");

            constructHeaderFormat();
        }

        /// <summary>
        /// Creates a daily log, where the messages are sent to a dated log file.
        /// </summary>
        /// <param name="subFolderName"></param>
        /// <param name="useUtc"></param>
        /// <param name="autoFlush"></param>
        /// <returns></returns>
        public static LogFile CreateDailyLog(string logFolder, bool useUtc = true, bool useMutex  = false, bool autoFlush = true) {
            DateTime date = useUtc ? DateTime.UtcNow.Date : DateTime.Now.Date;
            var log = new LogFile(getDailyLogName(logFolder, date), useUtc, useMutex, autoFlush);
            log._dailyDate = date;
            log._isDaily = true;
            log.constructHeaderFormat();
            return log;
        }

        private static string getDailyLogName(string logFolder, DateTime date) {
            return Path.Combine(logFolder, string.Format("{0:yyyy-MM-dd}.log", date));
        }

        /// <summary>
        /// Creates a rolling log that is backed up (removing any previous backup) when the
        /// size exceeds maxSize at the point the LogFile is created.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="maxSize"></param>
        /// <param name="useUtc"></param>
        /// <param name="autoFlush"></param>
        /// <returns></returns>
        public static LogFile CreateRollingLog(string filename, int maxSize, bool useUtc = true, bool useMutex = false, bool autoFlush = true) {
            var info = new FileInfo(filename);
            if (info.Exists && info.Length > maxSize) {
                var bakname = Path.ChangeExtension(filename, "bak");
                if (File.Exists(bakname)) File.Delete(bakname);
                info.MoveTo(bakname);
            }
            return new LogFile(filename, useUtc, useMutex, autoFlush);
        }

        protected override void Dispose(bool disposing) {
            _output.Dispose();
            if (_mutex != null) _mutex.Dispose();
            base.Dispose(disposing);
        }

        public string InstanceId {
            get { return _instanceId; }
            set {
                _instanceId = value;
                constructHeaderFormat();
            }
        }

        /// <summary>
        /// True if a mutex is being used to lock the log file across processes.
        /// </summary>
        public bool UseMutex { get { return _mutex != null; } }

        private void writeLine(string format, params object[] args) {
            try {
                lock (_synchLock) {
                    if (_mutex != null) _mutex.WaitOne();
                    var now = UseUtc ? DateTime.UtcNow : DateTime.Now;
                    if (_isDaily && now.Date != _dailyDate) {
                        // It's past midnight, so start a new log file
                        _output.Dispose();
                        _filename = getDailyLogName(Path.GetDirectoryName(_filename), now.Date);
                        var fs = File.Open(_filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _output = new StreamWriter(fs);
                        _dailyDate = now.Date;
                    }
                    _output.Write(_headerFormat, now, _instanceId);
                    _output.WriteLine(format, args);
                    if (AutoFlush) _output.Flush();
                }
            } finally {
                if (_mutex != null) _mutex.ReleaseMutex();
            }
        }

        private void constructHeaderFormat() {
            string f = _isDaily ? "{0:HH:mm:ss} - " : "{0:dd-MM-yyyy HH:mm:ss} - ";
            if (!string.IsNullOrWhiteSpace(InstanceId)) f += "{1} - ";
            _headerFormat = f;
        }
        
        /// <summary>
        /// Flushes the output. This need not be called if AutoFlush is true.
        /// </summary>
        public override void Flush() {
            if (_mutex != null) _mutex.WaitOne();
            try {
                lock (_synchLock) _output.Flush();
            } finally {
                if (_mutex != null) _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Writes a line to the log file.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLine(string format, params object[] args) {
            writeLine(format, args);
        }

        public override void Write(string message) {
            writeLine(message);
        }

        public override void WriteLine(string message) {
            writeLine(message);
        }

        public static void Log(string format, params object[] args) {
            System.Diagnostics.Trace.WriteLine(string.Format(format, args));
        }

        /// <summary>
        /// Dumps the values of the object's properties to the Log File.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="o"></param>
        public void DumpObject(string message, object o) {
            WriteLine(assembleDump(message, o));
        }

        /// <summary>
        /// Dumps the values of the object's properties to Trace.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="o"></param>
        public static void Dump(string message, object o) {
            Log(assembleDump(message, o));
        }

        private static string assembleDump(string message, object o) {
            if (o == null) {
                return string.Format("{0}: NULL", message);
            }
            StringBuilder s = new StringBuilder(message);
            s.AppendLine();
            foreach (var prop in o.GetType().GetProperties()) {
                try {
                    var val = prop.GetValue(o, null);
                    if (val == null) {
                        s.AppendFormat("    {0} is null", prop.Name);
                    } else if (val is string) {
                        s.AppendFormat("    {0}: '{1}'", prop.Name, val.ToString());
                    } else {
                        s.AppendFormat("    {0}: {1}", prop.Name, val.ToString());
                    }
                    s.AppendLine();
                } catch { }
            }
            return s.ToString();
        }

        /// <summary>
        /// Gets or sets whether this log file object is a trace listener, in which case it
        /// will automatically log anything written by the Trace object.
        /// </summary>
        public bool IsTraceListener {
            get { return System.Diagnostics.Trace.Listeners.Contains(this); }
            set {
                if (value && !IsTraceListener) System.Diagnostics.Trace.Listeners.Add(this);
                if (!value && IsTraceListener) System.Diagnostics.Trace.Listeners.Remove(this);
            }
        }
    }
}

