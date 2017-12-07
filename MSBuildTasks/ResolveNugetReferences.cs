using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml;
using System.IO;

namespace MSBuildTasks
{
    public class ResolveNugetReferences : Task
    {
        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(PackagesConfig) || !File.Exists(PackagesConfig))
            {
                Log.LogWarning($"File not found: {PackagesConfig}");
                return true;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(PackagesConfig);
            foreach (XmlNode n in xmlDoc.DocumentElement.ChildNodes)
            {
                if (!((n is XmlElement) && n.LocalName.Equals("package")))
                {
                    continue;
                }

                string id = n.Attributes["id"]?.Value;
                string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(PackagesConfig));
                string refFolder = Path.Combine(rootFolder, id);
                if (!Directory.Exists(refFolder))
                {
                    Log.LogMessage($"Directory not found when attempting to resolve nuget dependencies as project references: {refFolder}");
                    continue;
                }

                string[] refProjects = Directory.GetFiles(refFolder, $"{id}.*proj", SearchOption.TopDirectoryOnly);
                if (refProjects?.Length > 1)
                {
                    Log.LogWarning($"Too many files matching {refFolder}\\{id}.*proj when attempting to resolve nuget dependencies as project references. Picking one");
                }

                if (refProjects?.Length > 0)
                {
                    string refProj = refProjects[0];
                    Log.LogMessage($"Detected reference project: {refProj}");
                    targetOutputs_.Add(new TaskItem(refProj));
                }
            }

            return true;
        }

        [Required]
        public string PackagesConfig { get; set; }

        [Output]
        public ITaskItem[] TargetOutputs
        {
            get
            { 
                return targetOutputs_.ToArray();
            }
        }
        private List<ITaskItem> targetOutputs_ = new List<ITaskItem>();
    }
}
