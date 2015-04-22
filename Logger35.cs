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

// .NET 3.5 compatibility

namespace Babbacombe.Logger {

    static class StreamExtensions {
        public static void CopyTo(this Stream from, Stream to, int bufSize = 81920) {
            var buf = new byte[bufSize];
            int count;
            do {
                count = from.Read(buf, 0, bufSize);
                if (count > 0) to.Write(buf, 0, count);
            } while (count > 0);
        }
    }

    class SmtpClient : System.Net.Mail.SmtpClient, IDisposable {
        public SmtpClient(string server, int port) : base(server, port) { }

        // Ideally, this would send a QUIT, like the .net 45 version, but it can't.
        public void Dispose() { }
    }
}
