using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.Logger {
    public class InfoGatherer {

        public string GatherInfo() {
            try {
                StringBuilder info = new StringBuilder();
                // Reversed because the active form comes last.
                foreach (var f in Application.OpenForms.Cast<Form>().Reverse()) {
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
                if (string.IsNullOrWhiteSpace(finfo)) return;
                if (finfo.Contains('\n')) {
                    finfo = "    " + finfo.Replace("\n", "    \n");
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

    public interface IInfoGathering {
        string GatherInfo();
    }
}
