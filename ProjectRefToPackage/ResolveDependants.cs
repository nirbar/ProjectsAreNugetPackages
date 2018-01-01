using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using CoffmanGrahamScheduler;

namespace ProjectRefToPackage
{
    /// <summary>
    /// Order projects by build order, assuming each project's packages.config contain dependencies on other projects.
    /// </summary>
    public class ResolveDependants : Task, ICancelableTask
    {
        private bool cancel_ = false;

        public ResolveDependants()
        {
            ProcessorCount = Environment.ProcessorCount;
        }

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
                HashSet<PackagesConfig> buildSet = new HashSet<PackagesConfig>();
                foreach (PackagesConfig pc in projPackages.Values)
                {
                    List<string> pcDep = pc.ResolveRecursiveDependencies(projPackages);
                    if (pcDep.Contains(targetId) && projPackages.ContainsKey(pc.Id) && !buildSet.Contains(pc))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Build requires '{pc.Id}'.");
                        buildSet.Add(pc);
                    }

                    if (cancel_)
                    {
                        Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                        return false;
                    }
                }

                // Resolve projects build order.

                List<TaskItem> orderded = SetBuildOrder(buildSet, pkg2Items);
                if (orderded == null)
                {
                    return false;
                }
                DependantProjectsBuildOrdered = orderded.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        class BuildItem : Vertex
        {
            internal TaskItem taskItem_;
            internal PackagesConfig pkgCfg_;

            public override string ToString() => taskItem_.ItemSpec;
        };

        private List<TaskItem> SetBuildOrder(HashSet<PackagesConfig> buildSet, Dictionary<PackagesConfig, ITaskItem> pkg2Items)
        {
            HashSet<Vertex> vertices = new HashSet<Vertex>();

            ITaskItem tgtPrj = AllProjects.FirstOrDefault((i) => (i.GetMetadata("FullPath") == DependecyProject.GetMetadata("FullPath")));
            BuildItem tgtItem = new BuildItem();
            tgtItem.taskItem_ = new TaskItem(tgtPrj);

            // Create vertex list
            foreach (PackagesConfig pc in buildSet)
            {
                BuildItem bi = new BuildItem();
                bi.taskItem_ = new TaskItem(pkg2Items[pc]);
                bi.pkgCfg_ = pc;
                vertices.Add(bi);
            }

            // Set dependencies on projects that are to be built
            foreach (Vertex v in vertices)
            {
                BuildItem bi = v as BuildItem;
                bi.Dependencies.Add(tgtItem);
                bi.Dependencies.AddRange(
                    from vi in vertices
                    where ((vi != bi) && bi.pkgCfg_.Dependencies.Contains(((BuildItem)vi).pkgCfg_.Id))
                    select vi
                    );
            }

            // Ordered list. Target project is first in line
            List<TaskItem> orderded = new List<TaskItem>();

            vertices.Add(tgtItem);

            if (ProcessorCount <= 0)
            {
                ProcessorCount = Environment.ProcessorCount;
            }
            Graph g = Graph.CoffmanGraham(vertices, ProcessorCount);

            int l = 0;
            foreach (HashSet<Vertex> level in g.Levels)
            {
                foreach (Vertex v in level)
                {
                    BuildItem bi = v as BuildItem;
                    bi.taskItem_.SetMetadata("ParallelBuildLevel", l.ToString());
                    orderded.Add(bi.taskItem_);
                }
                ++l;
            }

            return orderded;
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

        public int ProcessorCount { get; set; }

        [Output]
        public ITaskItem[] DependantProjectsBuildOrdered { get; private set; }
    }
}
