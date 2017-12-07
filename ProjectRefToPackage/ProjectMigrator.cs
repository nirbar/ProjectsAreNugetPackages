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
        //TODO: Solution
        Dictionary<string, string> globalProps_ = new Dictionary<string, string>();

        public ProjectMigrator(string prjFile)
        {
            if (!File.Exists(prjFile))
            {
                throw new FileNotFoundException(prjFile);
            }

            projectFile_ = prjFile;
            projectFolder_ = Path.GetDirectoryName(projectFile_);

            packagesConfig_ = new PackagesConfig(Path.Combine(projectFolder_, "packages.config"));

            globalProps_["SolutionDir"] = Path.GetDirectoryName(projectFolder_);
            project_ = new Project(projectFile_, globalProps_, null);
        }

        public void MigrateProjectReferences()
        {
            List<ProjectItem> allProjReferences = new List<ProjectItem>();

            foreach(ProjectItem i in project_.AllEvaluatedItems)
            {
                if (i.ItemType.Equals("ProjectReference"))
                {
                    allProjReferences.Add(i);
                    string refProj = i.GetMetadataValue("FullPath");

                    if (File.Exists(refProj))
                    {
                        packagesConfig_.Add(Path.GetFileNameWithoutExtension(refProj));
                    }
                }
            }

            project_.RemoveItems(allProjReferences);
            packagesConfig_.Save();
            project_.Save();
        }
    }
}
