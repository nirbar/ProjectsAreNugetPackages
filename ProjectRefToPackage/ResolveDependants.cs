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
                HashSet<PackagesConfig> allPackages = GetAllPackages();
                if (allPackages == null)
                {
                    return false;
                }

                HashSet<PackagesConfig> buildSet = ResolveProjectsToBuild(allPackages);
                if (buildSet == null)
                {
                    return false;
                }

                List<TaskItem> orderded = ResolveBuildOrder(buildSet);
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

        private HashSet<PackagesConfig> ResolveProjectsToBuild(HashSet<PackagesConfig> allPackages)
        {
            // Detect projects that depend on a target project.
            bool buildAll = ((DependecyProjects == null) || (DependecyProjects.Count() == 0));
            if (buildAll)
            {
                Log.LogMessage(MessageImportance.Low, "Building all projects since DependecyProjects list is empty");
                return new HashSet<PackagesConfig>(allPackages);
            }

            HashSet<PackagesConfig> buildSet = new HashSet<PackagesConfig>();
            foreach (ITaskItem depProj in DependecyProjects)
            {
                if (cancel_)
                {
                    Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                    return null;
                }

                string targetProject = depProj.GetMetadata("FullPath");
                if (string.IsNullOrWhiteSpace(targetProject) || !File.Exists(targetProject))
                {
                    Log.LogError($"FullPath not found for target project {depProj.ItemSpec}");
                    return null;
                }

                Log.LogMessage(MessageImportance.Low, $"Inspecting target project '{depProj.ItemSpec}'.");

                PackagesConfig tgtPc = allPackages.FirstOrDefault((pc) => pc.ProjectTaskItem.GetMetadata("FullPath")?.Equals(targetProject) ?? false);
                if (tgtPc == null)
                {
                    Log.LogWarning($"Did not find target project '{depProj.ItemSpec}' in AllProjects list. Have you defined item metadata DependecyProjects?");
                    tgtPc = PackagesConfig.Create(depProj, PackageIdPrefix);
                    allPackages.Add(tgtPc);
                }

                buildSet.Add(tgtPc);
                foreach (PackagesConfig pc in allPackages)
                {
                    if (pc.Id == tgtPc.Id)
                    {
                        continue;
                    }

                    List<string> pcDep = pc.ResolveRecursiveDependencies(allPackages);
                    if (pcDep.Contains(tgtPc.Id) && !buildSet.Contains(pc))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Build requires '{pc.Id}'.");
                        buildSet.Add(pc);
                    }

                    if (cancel_)
                    {
                        Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                        return null;
                    }
                }
            }
            return buildSet;
        }

        private List<TaskItem> ResolveBuildOrder(HashSet<PackagesConfig> buildSet)
        {
            HashSet<Vertex> vertices = new HashSet<Vertex>();

            // Create vertex list
            foreach (PackagesConfig pc in buildSet)
            {
                BuildItem bi = new BuildItem();
                bi.taskItem_ = new TaskItem(pc.ProjectTaskItem);
                bi.pkgCfg_ = pc;
                vertices.Add(bi);
            }

            // Set dependencies on projects that are to be built
            foreach (Vertex v in vertices)
            {
                BuildItem bi = v as BuildItem;
                bi.Dependencies.AddRange(
                    from vi in vertices
                    where ((vi != bi) && bi.pkgCfg_.Dependencies.Contains(((BuildItem)vi).pkgCfg_.Id))
                    select vi
                    );
            }

            // Ordered list of projects to build.
            if (ProcessorCount <= 0)
            {
                ProcessorCount = Environment.ProcessorCount;
            }
            Graph g = Graph.CoffmanGraham(vertices, ProcessorCount);

            int l = 0;
            List<TaskItem> orderded = new List<TaskItem>();
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

        private HashSet<PackagesConfig> GetAllPackages()
        {
            HashSet<PackagesConfig> allPackages = new HashSet<PackagesConfig>();

            // Accumulate package dependencies for all projects.
            foreach (ITaskItem p in AllProjects)
            {
                string projPath = p.GetMetadata("FullPath");
                Log.LogMessage(MessageImportance.Low, $"Inspecting '{projPath}'.");
                if (string.IsNullOrWhiteSpace(projPath) || !File.Exists(projPath))
                {
                    Log.LogWarning($"FullPath not found for project {p.ItemSpec}");
                    continue;
                }

                // Project already handled?
                string projId = PackageIdPrefix + PackagesConfig.GetProjectId(p);
                if (allPackages.Any((i) => projId.Equals(i.Id)))
                {
                    continue;
                }

                // package.config file exists?
                PackagesConfig pc = PackagesConfig.Create(p, PackageIdPrefix);
                if (pc == null)
                {
                    Log.LogMessage(MessageImportance.Low, $"Can't find packages.config file for '{p.ItemSpec}'.");
                    continue;
                }

                Log.LogMessage(MessageImportance.Low, $"Parsing dependencies of '{p.ItemSpec}'.");
                allPackages.Add(pc);

                if (cancel_)
                {
                    Log.LogMessage(MessageImportance.Low, "Exit on cancel signal");
                    return null;
                }
            }

            return allPackages;
        }

        void ICancelableTask.Cancel()
        {
            cancel_ = true;
        }

        public ITaskItem[] DependecyProjects { get; set; }

        [Required]
        public ITaskItem[] AllProjects { get; set; }

        public string PackageIdPrefix { get; set; }

        public int ProcessorCount { get; set; }

        [Output]
        public ITaskItem[] DependantProjectsBuildOrdered { get; private set; }
    }
}
