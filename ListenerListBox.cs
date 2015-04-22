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
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Babbacombe.Logger {

    /// <summary>
    /// A list box that displays the first line of any Trace messages. The full
    /// message can be obtained from the SelectedItem (eg in a double click event).
    /// </summary>
    public partial class ListenerListBox : ListBox {
        private Listener _listener;

        /// <summary>
        /// The maximum number of messages to store and display. If the limit
        /// is exceeded, the earliest messages are discarded. If Zero, the default,
        /// there is no limit except that imposed by the ListBox.
        /// </summary>
        [DefaultValue(0), Description("The maximum number of items stored. Zero for no limit")]
        public int MaxItems { get; set; }

        /// <summary>
        /// Constructor for the Listener List Box control.
        /// </summary>
        public ListenerListBox() {
            InitializeComponent();

            // Create the handle here to ensure that InvokeRequired works
            // before the form is shown.
            CreateHandle();

            _listener = new Listener(this);
        }

        private void displayMessage(string message) {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(displayMessage), message);
                return;
            }
            int visibleCount = ClientSize.Height / ItemHeight;
            bool autoScroll = TopIndex + visibleCount >= base.Items.Count;
            base.Items.Add(new Item(message));
            if (autoScroll) TopIndex++;
            if (MaxItems > 0 && base.Items.Count > MaxItems) {
                while (base.Items.Count > MaxItems) base.Items.RemoveAt(0);
            }
        }

        /// <summary>
        /// Type of the items in the ListenerListBox.
        /// </summary>
        public class Item {
            /// <summary>
            /// The full Trace message.
            /// </summary>
            public string Message { get; private set; }

            /// <summary>
            /// The time (utc) the message was logged.
            /// </summary>
            public DateTime Time { get; private set; }

            internal Item(string message) {
                Message = message;
                Time = DateTime.UtcNow;
            }

            /// <summary>
            /// Returns the first line of the message.
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return Message == null ? null : Message.Split('\r', '\n')[0];
            }
        }

        /// <summary>
        /// The typed items in the list box.
        /// </summary>
        [Browsable(false)]
        public new IEnumerable<Item> Items {
            get { return base.Items.Cast<Item>(); }
        }

        /// <summary>
        /// The typed item selected in the list box.
        /// </summary>
        [Browsable(false)]
        public new Item SelectedItem {
            get { return (Item)base.SelectedItem; }
            set { base.SelectedItem = value; }
        }

        private class Listener : System.Diagnostics.TraceListener {
            private ListenerListBox _listBox;
            private StringBuilder _buf = new StringBuilder();
            private object _lock = new object();

            public Listener(ListenerListBox listBox) {
                _listBox = listBox;
                System.Diagnostics.Trace.Listeners.Add(this);
            }

            public override void Write(string message) {
                lock (_lock) _buf.Append(message);
            }

            public override void WriteLine(string message) {
                lock (_lock) {
                    if (_buf.Length > 0) message = _buf.ToString() + message;
                    _buf.Length = 0;
                    _listBox.displayMessage(message);
                }
            }
        }
    }
}
