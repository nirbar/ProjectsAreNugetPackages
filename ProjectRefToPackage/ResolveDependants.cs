using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace ProjectRefToPackage
{
    /// <summary>
    /// Order projects by build order, assuming each project's packages.config contain dependencies on other projects.
    /// </summary>
    public class ResolveDependants : Task, ICancelableTask
    {
        private bool cancel_ = false;

        public override bool Execute()
        {
            try
            {
                if (cancel_)
                {
                    Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                    return false;
                }

                Dictionary<string, PackagesConfig> projPackages = new Dictionary<string, PackagesConfig>();
                Dictionary<PackagesConfig, ITaskItem> pkg2Items = new Dictionary<PackagesConfig, ITaskItem>();

                string targetProject = DependecyProject.GetMetadata("FullPath");
                if (string.IsNullOrWhiteSpace(targetProject) || !File.Exists(targetProject))
                {
                    Log.LogError($"FullPath not found for target project {DependecyProject.ItemSpec}");
                    return false;
                }

                Log.LogMessage(MessageImportance.Low, $"First project is '{DependecyProject.ItemSpec}'. All projects count is {AllProjects.Length}");

                // Accumulate package dependencies for all projects.
                foreach (ITaskItem p in AllProjects)
                {
                    string projPath = p.GetMetadata("FullPath");
                    Log.LogMessage(MessageImportance.Low, $"Inspecting '{projPath}'.");
                    if (string.IsNullOrWhiteSpace(projPath) || !File.Exists(projPath))
                    {
                        Log.LogWarning($"FullPath not found for target project {p.ItemSpec}");
                        continue;
                    }

                    // Project already handled?
                    string projId = PackageIdPrefix + Path.GetFileNameWithoutExtension(projPath);
                    if (projPackages.ContainsKey(projId))
                    {
                        continue;
                    }

                    // package.config file exists?
                    string pkgCfg = Path.Combine(Path.GetDirectoryName(projPath), "packages.config");
                    if (!File.Exists(pkgCfg))
                    {
                        Log.LogMessage(MessageImportance.Low, $"File not found '{pkgCfg}'.");
                        continue;
                    }

                    Log.LogMessage(MessageImportance.Low, $"Parsing '{pkgCfg}'.");
                    PackagesConfig pc = new PackagesConfig(pkgCfg);
                    pc.PackageIdPrefix = PackageIdPrefix;
                    projPackages[projId] = pc;
                    pkg2Items[pc] = p;

                    if (cancel_)
                    {
                        Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                        return false;
                    }
                }

                string targetId = PackageIdPrefix + Path.GetFileNameWithoutExtension(targetProject);
                List<PackagesConfig> pcOrder = new List<PackagesConfig>();
                foreach (PackagesConfig pc in projPackages.Values)
                {
                    List<string> pcDep = pc.ResolveRecursiveDependencies(projPackages);
                    if (pcDep.Contains(targetId) && projPackages.ContainsKey(pc.Id) && !pcOrder.Contains(pc))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Build requires '{pc.Id}'.");
                        pcOrder.Add(pc);
                    }

                    if (cancel_)
                    {
                        Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                        return false;
                    }
                }

                // Resolve projects build order.
                if (!SetBuildOrder(pcOrder))
                {
                    return false;
                }

                List<TaskItem> orderded = new List<TaskItem>();

                ITaskItem tgtPrj = AllProjects.FirstOrDefault((i) => (i.GetMetadata("FullPath") == DependecyProject.GetMetadata("FullPath")));
                orderded.Add(new TaskItem(tgtPrj ?? DependecyProject));
                foreach (PackagesConfig pc in pcOrder)
                {
                    TaskItem t = new TaskItem(pkg2Items[pc]);
                    Log.LogMessage($"Build order: {t.ItemSpec}");
                    orderded.Add(t);
                }

                DependantProjectsBuildOrdered = orderded.ToArray();
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
            return !cancel_;
        }

        private bool SetBuildOrder(List<PackagesConfig> pcOrder)
        {
            bool changed;
            int changeCount = 0;
            int maxChanges = pcOrder.Count * pcOrder.Count;

            Log.LogMessage(MessageImportance.Low, $"Resolving Build order. Local projects are prefixed with '{PackageIdPrefix}' on packages.config");

            do
            {
                if (cancel_)
                {
                    Log.LogMessage("Exiting on cancel signal");
                    return false;
                }

                if (changeCount > maxChanges)
                {
                    Log.LogError("Project dependency dead-lock. There's a circular dependency in projects.");
                    return false;
                }
                changed = false;

                for (int i = 0; i < pcOrder.Count; ++i)
                {
                    PackagesConfig proj1 = pcOrder[i];
                    for (int j = i + 1; j < pcOrder.Count; ++j)
                    {
                        if (cancel_)
                        {
                            Log.LogMessage("Exiting on cancel signal");
                            return false;
                        }

                        PackagesConfig proj2 = pcOrder[j];
                        if (proj1.ResolveRecursiveDependencies(null).Contains(proj2.Id, StringComparer.OrdinalIgnoreCase))
                        {
                            Log.LogMessage(MessageImportance.Low, $"'{proj1.Id}' depends on '{proj2.Id}'");

                            changed = true;
                            pcOrder.RemoveAt(j);
                            pcOrder.Insert(i, proj2);
                            break;
                        }
                    }

                    if (changed)
                    {
                        ++changeCount;
                        break;
                    }
                }
            } while (changed);

            return true;
        }

        void ICancelableTask.Cancel()
        {
            cancel_ = true;
        }

        [Required]
        public ITaskItem DependecyProject { get; set; }

        [Required]
        public ITaskItem[] AllProjects { get; set; }

        public string PackageIdPrefix { get; set; }

        [Output]
        public ITaskItem[] DependantProjectsBuildOrdered { get; private set; }
    }
}
