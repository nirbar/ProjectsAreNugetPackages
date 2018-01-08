using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace ProjectRefToPackage
{
    public class ConvertProjectRefToNugetPackage : Task
    {
        public ConvertProjectRefToNugetPackage()
        {
            PackageVersion = "0.0.0";
        }

        public override bool Execute()
        {
            string solutionDir = SolutionDir.GetMetadata("FullPath");
            if (string.IsNullOrWhiteSpace(solutionDir) || !Directory.Exists(solutionDir))
            {
                Log.LogError($"SolutionDir not found: '{SolutionDir.ItemSpec}'");
                return false;
            }

            foreach (ITaskItem prjFile in Projects)
            {
                string prjPath = prjFile.GetMetadata("FullPath");
                if (string.IsNullOrWhiteSpace(prjPath) || !File.Exists(prjPath))
                {
                    Log.LogError($"Project not found: '{prjFile.ItemSpec}'");
                    continue;
                }

                Dictionary<string, string> globalProps = new Dictionary<string, string>();
                foreach (ITaskItem i in Properties)
                {
                    int ii = i.ItemSpec.IndexOf('=');
                    if (ii > 0)
                    {
                        string k = i.ItemSpec.Substring(0, ii);
                        string v = i.ItemSpec.Substring(1 + ii);
                        globalProps[k] = v;
                    }
                }

                Log.LogMessage($"Converting project references to nuget dependency packages for '{prjFile.ItemSpec}'");
                ProjectMigrator mgrt = new ProjectMigrator(Log, prjPath, AllProjects, globalProps, PackageVersion);
                mgrt.PackageIdPrefix = PackageIdPrefix;
                mgrt.MigrateProjectReferences();
            }

            return true;
        }

        [Required]
        public ITaskItem[] Projects { get; set; }

        [Required] 
        public ITaskItem SolutionDir { get; set; }

        [Required]
        public ITaskItem[] AllProjects { get; set; }

        public ITaskItem[] Properties { get; set; }

        public string PackageIdPrefix { get; internal set; }

        public string PackageVersion { get; internal set; }
    }
}
