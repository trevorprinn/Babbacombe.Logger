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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Babbacombe.Logger {

    /// <summary>
    /// Gathers debug information about and from the forms open in the application.
    /// </summary>
    public class InfoGatherer {

        /// <summary>
        /// Gathers the information about the forms, and returns it as a string.
        /// </summary>
        /// <returns></returns>
        public string GatherInfo() {
            try {
                StringBuilder info = new StringBuilder();
                // Reversed because the active form comes last.
                foreach (var f in Application.OpenForms.Cast<Form>().Reverse()) {
                    if (f.IsMdiChild) continue;
                    gatherFormInfo(info, f);
                    if (f.IsMdiContainer) {
                        foreach (var c in f.MdiChildren) {
                            gatherFormInfo(info, c);
                        }
                    }
                }
                return info.ToString().TrimEnd('\r', '\n');
            } catch (Exception ex) {
                LogFile.Log(ex.ToString());
                return "Could not Gather Info";
            }
        }

        private void gatherFormInfo(StringBuilder info, Form f) {
            try {
                var gathering = f as IInfoGathering;
                info.AppendFormat("{0} [{1}] '{2}'", f.IsMdiContainer ? "Mdi Container" : f.IsMdiChild ? "Mdi Child" : "Form", f.GetType().FullName, f.Text);
                string finfo;
                try {
                    finfo = gathering == null ? null : gathering.GatherInfo();
                } catch (Exception ex) {
                    LogFile.Log(ex.ToString());
                    finfo = "Exception: " + ex.Message;
                }
#if NET35
                if (string.IsNullOrEmpty(finfo)) return;
#else
                if (string.IsNullOrWhiteSpace(finfo)) return;
#endif
                if (finfo.Contains('\n')) {
                    finfo = "    " + Regex.Replace(finfo, @"\n|(\r\n)", "$0    ");
                    info.AppendLine();
                    info.Append(finfo);
                } else {
                    info.AppendFormat(" - {0}", finfo);
                }
            } finally {
                info.AppendLine();
            }
        }
    }

    /// <summary>
    /// Interface that can be added to a form in the application to return extra debug information.
    /// </summary>
    public interface IInfoGathering {
        /// <summary>
        /// Gathers any extra information from the form.
        /// </summary>
        /// <returns></returns>
        string GatherInfo();
    }
}
