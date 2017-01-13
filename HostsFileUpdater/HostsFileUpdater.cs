using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net;

namespace HostsFileUpdater
{
    public partial class HostsFileUpdater : ServiceBase
    {
        public HostsFileUpdater()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.UpdateHostsFile();
        }

        protected override void OnStop()
        {

        }

        private void UpdateHostsFile()
        {
            var HostsFile = Environment.ExpandEnvironmentVariables("%windir%\\system32\\drivers\\etc\\hosts");
            var OrigHostsFile = HostsFile + ".orig";

            try
            {
                if (!File.Exists(OrigHostsFile))
                {
                    File.Copy(HostsFile, OrigHostsFile);
                }
            }
            catch(IOException ioex)
            {
                var message = $"Failed to create a copy of the hosts file:\n\n{ioex.Message}";
                EventLog.WriteEntry("HostsFileUpdater", message, EventLogEntryType.Error);
            }

            try
            {
                var HostsFileContent = new StringBuilder("");
                var XMLDoc = XDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml"));
                var HostFileSources = XMLDoc.Descendants("HostFile");
                foreach(var HostFileSouce in HostFileSources)
                {
                    var Location = HostFileSources.Descendants("URL").FirstOrDefault();
                    if(Location != null)
                    {
                        using (var client = new WebClient())
                        {
                            EventLog.WriteEntry("HostsFileUpdater", $"Downloading host file from: {Location.Value}", EventLogEntryType.Information);
                            HostsFileContent.Append(client.DownloadString(Location.Value));
                            HostsFileContent.AppendLine();
                        }
                    }
                }

                File.WriteAllText(HostsFile, HostsFileContent.ToString());
            }
            catch(Exception ex)
            {
                EventLog.WriteEntry("HostsFileUpdater", ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
