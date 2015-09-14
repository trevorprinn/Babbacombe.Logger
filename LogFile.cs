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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Babbacombe.Logger {
    /// <summary>
    /// Manages writing to a log file. The output stream is kept open as long as the LogFile
    /// object exists. The log file can be read externally while the LogFile object has it open.
    /// All messages written are prefixed with the current date time. The log file can be made a trace
    /// listener, so that all Trace messages are written to it, and contains a static method for
    /// writing to Trace.
    /// </summary>

    public class LogFile : TraceListener {
        private object _synchLock = new object();
        private StreamWriter _output;
        private string _filename;
        private string _headerFormat;

        /// <summary>
        /// If logging fails, the Logger will attempt to write a message to the
        /// Application log using this source name, which will default to the Entry
        /// Assembly name.
        /// </summary>
        /// <remarks>
        /// Writing to the event log will fail silently on Vista or later unless the source
        /// has already been added, or the application has been run as Administrator.
        /// It can most easily be added at installation by using EventLog.SourceExists
        /// and EventLog.CreateEventSource.
        /// </remarks>
        public static string EventLogSource { get; set; }

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
        /// <param name="filename">The full path of the log file</param>
        /// <param name="useUtc">Whether to use UTC or local time. Defaults to true.</param>
        /// <param name="useMutex">
        /// Whether to use a Mutex to control concurrent writing to the log file by another application
        /// or multiple instances of the same application. Defaults to false.
        /// </param>
        /// <param name="autoFlush">Whether to flush the log to disk after each message is written. Defaults to true.</param>
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
        /// <param name="logFolder">The subfolder to write the logs to. Must already exist.</param>
        /// <param name="useUtc">Whether to use UTC or local time. Defaults to true.</param>
        /// <param name="useMutex">
        /// Whether to use a Mutex to control concurrent writing to the log file by another application
        /// or multiple instances of the same application. Defaults to false.
        /// </param>
        /// <param name="autoFlush">Whether to flush the log to disk after each message is written. Defaults to true.</param>
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
        /// <param name="filename">The fuill path of the log file.</param>
        /// <param name="maxSize">The maximum size after which to create a new log file.</param>
        /// <param name="useUtc">Whether to use UTC or local time. Defaults to true.</param>
        /// <param name="useMutex">
        /// Whether to use a Mutex to control concurrent writing to the log file by another application
        /// or multiple instances of the same application. Defaults to false.
        /// </param>
        /// <param name="autoFlush">Whether to flush the log to disk after each message is written. Defaults to true.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Closes the output stream and disposes of any mutex.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            _output.Dispose();
#if NET35
            if (_mutex != null) _mutex.Close();
#else
            if (_mutex != null) _mutex.Dispose();
#endif
            base.Dispose(disposing);
        }

        /// <summary>
        /// Sets or gets an optional identifier for the application or instance that
        /// will be added to each message, if set.
        /// </summary>
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
                writeLine(string.Format(format, args));
            } catch (Exception ex) {
                sendToEventLog(ex, format, args);
            }
        }

        private void writeLine(string message) {
            if (_mutex != null) _mutex.WaitOne();
            try {
                lock (_synchLock) {
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
                    _output.WriteLine(message);
                    if (AutoFlush) _output.Flush();
                }
            } catch (Exception ex) {
                sendToEventLog(ex, message);
            } finally {
                if (_mutex != null) _mutex.ReleaseMutex();
            }
        }

        private void constructHeaderFormat() {
            string f = _isDaily ? "{0:HH:mm:ss} - " : "{0:dd-MM-yyyy HH:mm:ss} - ";
#if NET35
            if (!string.IsNullOrEmpty(InstanceId)) f += "{1} - ";
#else
            if (!string.IsNullOrWhiteSpace(InstanceId)) f += "{1} - ";
#endif
            _headerFormat = f;
        }
        
        /// <summary>
        /// Flushes the output. This need not be called if AutoFlush is true.
        /// </summary>
        public override void Flush() {
            if (_mutex != null) _mutex.WaitOne();
            try {
                lock (_synchLock) _output.Flush();
            } catch (Exception ex) {
                sendToEventLog(ex, "Failed to Flush Log File");
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

        /// <summary>
        /// Writes the string to the log (NB writes it as a line).
        /// </summary>
        /// <param name="message"></param>
        public override void Write(string message) {
            writeLine(message);
        }

        /// <summary>
        /// Writes a line to the log.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message) {
            writeLine(message);
        }

        /// <summary>
        /// Writes a line to the Trace object, which will also log it if any LogFile
        /// objects are Trace Listeners.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(string format, params object[] args) {
            try {
                Trace.WriteLine(string.Format(format, args));
            } catch (Exception ex) {
                sendToEventLog(ex, format, args);
            }
        }

        /// <summary>
        /// Writes a line to the Trace object, which will also log it if any LogFile
        /// objects are Trace Listeners.
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message) {
            Trace.WriteLine(message);
        }

        /// <summary>
        /// Dumps the values of the object's properties to the Log File.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="o"></param>
        public void DumpObject(string message, object o) {
            try {
                WriteLine(assembleDump(message, o));
            } catch (Exception ex) {
                sendToEventLog(ex, message);
            }
        }

        /// <summary>
        /// Dumps the values of the object's properties to Trace.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="o"></param>
        public static void Dump(string message, object o) {
            try {
                Log(assembleDump(message, o));
            } catch (Exception ex) {
                sendToEventLog(ex, message);
            }
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
            get { return Trace.Listeners.Contains(this); }
            set {
                if (value && !IsTraceListener) Trace.Listeners.Add(this);
                if (!value && IsTraceListener) Trace.Listeners.Remove(this);
            }
        }

        private static void sendToEventLog(string message) {
            try {
                if (EventLogSource == null) {
                    EventLogSource = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                    // SourceExists/CreateEventSource won't work on Vista and later.
                    // After XP, the EventSource has to be created as Administrator.
                    if (!EventLog.SourceExists(EventLogSource)) EventLog.CreateEventSource(EventLogSource, "Application");
                }
                string logMessage = "Failed to log message:\r\n" + message;
                if (logMessage.Length > 30000) logMessage = logMessage.Substring(0, 29997) + "...";
                EventLog.WriteEntry(EventLogSource, logMessage, EventLogEntryType.Error);
            } catch { }
        }

        private static void sendToEventLog(Exception ex, string message) {
            try {
                string msg = string.Format("{0}\r\n\r\nOriginal Message:\r\n{1}", ex.ToString(), message);
                sendToEventLog(msg);
            } catch { }
        }

        private static void sendToEventLog(Exception ex, string format, params object[] args) {
            try {
                StringBuilder msg = new StringBuilder();
                msg.AppendLine(ex.ToString());
                msg.AppendLine();
                msg.AppendLine("Original Message:");
                msg.AppendLine(format);
                if (args != null && args.Length > 0) {
                    for (int i = 0; i < args.Length; i++) {
                        object arg = args[i];
                        msg.AppendFormat("{0}: {1}\r\n", i, arg == null ? "null" : arg.ToString());
                    }
                    msg.Length -= 2;
                }
                sendToEventLog(msg.ToString());
            } catch { }
        }
    }
}

