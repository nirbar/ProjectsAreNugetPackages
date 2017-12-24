using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using System.IO;

namespace ProjectRefToPackage
{
    class ProjectMigrator
    {
        Project project_;
        string projectFile_;
        string projectFolder_;
        PackagesConfig packagesConfig_;
        Dictionary<string, string> globalProps_ = new Dictionary<string, string>();

        public ProjectMigrator(string prjFile, string solutionDir = null)
        {
            if (!File.Exists(prjFile))
            {
                throw new FileNotFoundException(prjFile);
            }

            if (string.IsNullOrWhiteSpace(solutionDir))
            {
                solutionDir = Path.GetDirectoryName(prjFile);
            }

            projectFile_ = prjFile;
            globalProps_["SolutionDir"] = solutionDir;
        }

        private void Initialize()
        {
            projectFolder_ = Path.GetDirectoryName(projectFile_);
            packagesConfig_ = new PackagesConfig(Path.Combine(projectFolder_, "packages.config"));
            packagesConfig_.PackageIdPrefix = PackageIdPrefix;
            project_ = new Project(projectFile_, globalProps_, null);
        }

        public void MigrateProjectReferences()
        {
            List<ProjectItem> allProjReferences = new List<ProjectItem>();

            Initialize();

            foreach (ProjectItem i in project_.AllEvaluatedItems)
            {
                if (i.ItemType.Equals("ProjectReference"))
                {
                    allProjReferences.Add(i);
                    string refProj = i.GetMetadataValue("FullPath");

                    if (!string.IsNullOrWhiteSpace(refProj) && File.Exists(refProj))
                    {
                        Console.WriteLine($"Project {projectFile_}- Converting project reference {refProj} to Nuget package");
                        packagesConfig_.Add(PackageIdPrefix + Path.GetFileNameWithoutExtension(refProj));
                    }
                }
            }

            project_.RemoveItems(allProjReferences);
            packagesConfig_.Save();
            project_.Save();
        }

        public string PackageIdPrefix { get; internal set; }
    }
}
