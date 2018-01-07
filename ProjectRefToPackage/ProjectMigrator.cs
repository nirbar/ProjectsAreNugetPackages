using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using System.IO;
using Microsoft.Build.Utilities;
using System.Linq;

namespace ProjectRefToPackage
{
    class ProjectMigrator
    {
        Project project_;
        string projectFile_;
        string projectFolder_;
        List<string> allProjects_;
        PackagesConfig packagesConfig_;
        Dictionary<string, string> globalProps_ = new Dictionary<string, string>();
        TaskLoggingHelper logger_;

        public ProjectMigrator(TaskLoggingHelper logger, string prjFile, string[] allProjects)
        {
            logger_ = logger;
            if (!File.Exists(prjFile))
            {
                throw new FileNotFoundException(prjFile);
            }
            allProjects_ = new List<string>(allProjects);

            string solutionDir = Path.GetDirectoryName(prjFile);

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

            foreach (ProjectItem i in project_.GetItemsIgnoringCondition("ProjectReference"))
            {
                allProjReferences.Add(i);
                string refProj = i.GetMetadataValue("FullPath");

                if (!string.IsNullOrWhiteSpace(refProj) && File.Exists(refProj))
                {
                    logger_.LogMessage($"Project {projectFile_}- Converting project reference '{refProj}' to Nuget package");
                    packagesConfig_.Add(PackageIdPrefix + Path.GetFileNameWithoutExtension(refProj));
                }
            }

            foreach (ProjectItem i in project_.GetItemsIgnoringCondition("Link"))
            {
                string libDependencies = i.GetMetadataValue("AdditionalDependencies");
                string[] libs = libDependencies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string l in libs)
                {
                    string li = Path.GetFileNameWithoutExtension(l);
                    string refProj = allProjects_.Find((p) => ((!string.IsNullOrWhiteSpace(p)) && File.Exists(p) && Path.GetFileNameWithoutExtension(p).Equals(li, StringComparison.OrdinalIgnoreCase)));
                    if (!string.IsNullOrWhiteSpace(refProj))
                    {
                        logger_.LogMessage($"Project {projectFile_}- Converting library reference '{refProj}' to Nuget package");
                        packagesConfig_.Add(PackageIdPrefix + li);
                    }
                }
            }

            foreach (ProjectMetadata i in project_.AllEvaluatedItemDefinitionMetadata)
            {
                if (i.ItemType.Equals("Link") && i.Name.Equals("AdditionalDependencies"))
                {
                    string libDependencies = i.EvaluatedValue;
                    string[] libs = libDependencies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string l in libs)
                    {
                        string li = Path.GetFileNameWithoutExtension(l);
                        string refProj = allProjects_.Find((p) => ((!string.IsNullOrWhiteSpace(p)) && File.Exists(p) && Path.GetFileNameWithoutExtension(p).Equals(li, StringComparison.OrdinalIgnoreCase)));
                        if (!string.IsNullOrWhiteSpace(refProj))
                        {
                            logger_.LogMessage($"Project {projectFile_}- Converting library reference '{refProj}' to Nuget package");
                            packagesConfig_.Add(PackageIdPrefix + li);
                        }
                    }
                }
            }

            ProjectItemDefinition pid = project_.ItemDefinitions["Link"];
            if (pid != null)
            {
                string libDependencies = pid.GetMetadataValue("AdditionalDependencies");
                string[] libs = libDependencies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string l in libs)
                {
                    string li = Path.GetFileNameWithoutExtension(l);
                    string refProj = allProjects_.Find((p) => ((!string.IsNullOrWhiteSpace(p)) && File.Exists(p) && Path.GetFileNameWithoutExtension(p).Equals(li, StringComparison.OrdinalIgnoreCase)));
                    if (!string.IsNullOrWhiteSpace(refProj))
                    {
                        logger_.LogMessage($"Project {projectFile_}- Converting library reference '{refProj}' to Nuget package");
                        packagesConfig_.Add(PackageIdPrefix + li);
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