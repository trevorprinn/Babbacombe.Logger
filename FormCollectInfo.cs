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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Babbacombe.Logger {

    /// <summary>
    /// Form that can collect information from the user before sending a log.
    /// </summary>
    public partial class FormCollectInfo : Form {
        private LogSender _logSender;

        private FormCollectInfo() {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor for FormCollectInfo.
        /// </summary>
        /// <param name="logSender">The log sender to use to send the log information.</param>
        /// <param name="terminating">Whether the program will be exiting automatically.</param>
        public FormCollectInfo(LogSender logSender, bool terminating) {
            InitializeComponent();

            _logSender = logSender;
            _logSender.AddFiles += _logSender_AddFiles;

            labelSend.Text += Application.CompanyName;

            if (terminating) {
                checkExit.Checked = true;
                checkExit.Enabled = false;
            } else {
                labelExit.Hide();
            }
        }

        void _logSender_AddFiles(object sender, LogSender.AddFilesEventArgs e) {
            if (textName.Text.Trim() != "" || textEmail.Text.Trim() != null) {
                e.LogSender.SendString(e.ZipStream, "sender.txt", textName.Text + "\r\n" + textEmail.Text);
            }
            if (checkScreenshot.Checked) e.LogSender.SendScreenshot(e.ZipStream);
            if (textNotes.Text.Trim() != "") {
                e.LogSender.SendString(e.ZipStream, "notes.txt", textNotes.Text);
            }
        }

        private void btnSend_Click(object sender, EventArgs e) {
            var cursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try {
                if (_logSender.HasUnsentFiles) {
                    _logSender.SendUnsentFiles();
                }
                _logSender.Send();
                DialogResult = checkExit.Checked ? DialogResult.Abort : DialogResult.OK;
            } finally {
                Cursor.Current = cursor;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            DialogResult = checkExit.Checked ? DialogResult.Abort : DialogResult.OK;
        }
    }
}
