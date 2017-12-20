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

                Log.LogMessage($"Converting project references to nuget dependency packages for '{prjFile.ItemSpec}'");
                ProjectMigrator mgrt = new ProjectMigrator(prjPath);
                mgrt.MigrateProjectReferences();
            }

            return true;
        }

        [Required]
        public ITaskItem[] Projects { get; set; }

        [Required] 
        public ITaskItem SolutionDir { get; set; }
    }
}
