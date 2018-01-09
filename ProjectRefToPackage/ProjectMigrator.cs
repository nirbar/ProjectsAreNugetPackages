using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using System.IO;
using Microsoft.Build.Utilities;
using System.Linq;
using Microsoft.Build.Framework;

namespace ProjectRefToPackage
{
    class ProjectMigrator
    {
        Project project_;
        string projectFile_;
        string projectFolder_;
        HashSet<ITaskItem> allProjects_;
        PackagesConfig packagesConfig_;
        Dictionary<string, string> globalProps_;
        TaskLoggingHelper logger_;
        string packageVersion_;

        public ProjectMigrator(TaskLoggingHelper logger, ITaskItem proj, ITaskItem[] allProjects, string packageVersion)
        {
            logger_ = logger;
            packageVersion_ = packageVersion;
            string prjFile = proj.GetMetadata("FullPath");
            if (string.IsNullOrWhiteSpace(prjFile) || !File.Exists(prjFile))
            {
                throw new FileNotFoundException(proj.ItemSpec);
            }
            allProjects_ = new HashSet<ITaskItem>(allProjects);

            projectFile_ = prjFile;
            globalProps_ = new Dictionary<string, string>();
            if (!globalProps_.ContainsKey("SolutionDir"))
            {
                string solutionDir = Path.GetDirectoryName(prjFile);
                globalProps_["SolutionDir"] = solutionDir;
            }

            string moreProps = $"{proj.GetMetadata("Properties")};{proj.GetMetadata("AdditionalProperties")}";
            string[] morePropsList = moreProps.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string kv in morePropsList)
            {
                int i = kv.IndexOf('=');
                if (i > 0)
                {
                    string k = kv.Substring(0, i);
                    string v = kv.Substring(1 + i);
                    globalProps_[k] = v;
                }
            }
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
                    packagesConfig_.Add(PackageIdPrefix + Path.GetFileNameWithoutExtension(refProj), packageVersion_);
                }
            }

            foreach (ProjectItem i in project_.GetItemsIgnoringCondition("Link"))
            {
                string libDependencies = i.GetMetadataValue("AdditionalDependencies");
                string[] libs = libDependencies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string l in libs)
                {
                    string li = Path.GetFileNameWithoutExtension(l);
                    ConvertLibToPkg(li);
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
                        ConvertLibToPkg(li);
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
                    ConvertLibToPkg(li);
                }
            }

            project_.RemoveItems(allProjReferences);
            packagesConfig_.Save();
            project_.Save();
        }

        public string PackageIdPrefix { get; internal set; }

        private void ConvertLibToPkg(string libName)
        {
            logger_.LogMessage(MessageImportance.Low, $"Inspecting '{libName}'");
            foreach (ITaskItem i in allProjects_)
            {
                string pp = i.GetMetadata("FullPath");
                if (string.IsNullOrEmpty(pp) || !File.Exists(pp))
                {
                    continue;
                }

                if (Path.GetFileNameWithoutExtension(pp).Equals(libName, StringComparison.OrdinalIgnoreCase))
                {
                    logger_.LogMessage($"Project {projectFile_}- Converting library reference '{i.ItemSpec}' to Nuget package");
                    packagesConfig_.Add(PackageIdPrefix + libName, packageVersion_);
                    break;
                }

                string alias = i.GetMetadata("Alias");
                if (!string.IsNullOrEmpty(alias) && alias.Equals(libName, StringComparison.OrdinalIgnoreCase))
                {
                    logger_.LogMessage($"Project {projectFile_}- Converting library reference '{i.ItemSpec}' to Nuget package '{alias}'");
                    packagesConfig_.Add(PackageIdPrefix + alias, packageVersion_);
                    break;
                }
            }
        }
    }
}