﻿#region Licence
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.Logger {

    /// <summary>
    /// Adds top level error handlers that should be able to trap all
    /// exceptions that are not otherwise trapped. An object of this class
    /// should be created in the app's Main method.
    /// </summary>
    /// <remarks>
    /// Exceptions that occur while handling these exceptions are ignored, as there
    /// is no safe way to log them.
    /// </remarks>
    public class LastChanceHandler {

        /// <summary>
        /// Raised when an exception occurs on the main Application thread.
        /// Adding a handler for this will prevent the default handling occurring.
        /// </summary>
        public event EventHandler<ThreadExceptionEventArgs> ThreadException;

        /// <summary>
        /// Raised when an exception occurs on a thread other than the main Application thread.
        /// Adding a handler for this will prevent the default handling occurring.
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> GeneralException;

        /// <summary>
        /// Raised when an exception is trapped and the user chooses to close the application.
        /// This can be used to close the main window cleanly, for example.
        /// </summary>
        public event EventHandler AppClosing;

        [Flags]
        public enum Traps { Thread = 1, General = 2, Both = 3 };

        /// <summary>
        /// Adds handlers for otherwise untrapped exceptions.
        /// </summary>
        /// <param name="apply"></param>
        public LastChanceHandler(Traps apply = Traps.Both) {
            if ((apply & Traps.Thread) == Traps.Thread) {
                Application.ThreadException += (s, e) => OnThreadException(s, e);
            }
            if ((apply & Traps.General) == Traps.General) {
                AppDomain.CurrentDomain.UnhandledException += (s, e) => OnGeneralException(s, e);
            }
        }

        /// <summary>
        /// Handles untrapped exceptions on the main Application thread. If any EventHandlers have been
        /// added they are called, otherwise a message box is shown to the user with the option to close
        /// the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnThreadException(object sender, ThreadExceptionEventArgs e) {
            try {
                if (ThreadException != null) {
                    ThreadException(sender, e);
                } else {
                    LogFile.Log(e.Exception.ToString());
                    if (MessageBox.Show(e.Exception.Message + "\r\n\r\nPress Cancel to Exit", "Unexpected Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Cancel) {
                        try { OnAppClosing(); } catch { }
                        Environment.Exit(0);
                    }
                }
            } catch {
                // Can't do anything with an exception in here.
            }
        }

        /// <summary>
        /// Handles untrapped exceptions on threads other than the main Application thread. If any
        /// EventHandlers have been added they are called, otherwise a message box is shown to the user.
        /// If the event can be recovered from, which is unusual, the user is given the option to close
        /// the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnGeneralException(object sender, UnhandledExceptionEventArgs e) {
            try {
                if (GeneralException != null) {
                    GeneralException(sender, e);
                } else {
                    var ex = e.ExceptionObject as Exception;
                    if (ex == null) {
                        LogFile.Log("Unhandled exception with no Exception object");
                    } else {
                        LogFile.Log(ex.ToString());
                    }

                    string msg = ex == null ? "Unknown Exception" : ex.Message;
                    if (e.IsTerminating) {
                        MessageBox.Show(msg + "\r\n\r\nThe program is terminating.", "Unexpected Error");
                    } else if (MessageBox.Show(msg + "\r\n\r\nPress Cancel to Exit the program", "Unexpected Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1) == DialogResult.Cancel) {
                        try { OnAppClosing(); } catch { }
                        Environment.Exit(0);
                    }
                }
            } catch {
                // Can't do anything with an exception in here.
            }
        }

        /// <summary>
        /// Called when the user decides to close the application due to an exception trapped within
        /// this object.
        /// </summary>
        protected virtual void OnAppClosing() {
            if (AppClosing != null) AppClosing(this, EventArgs.Empty);
        }
    }
}
