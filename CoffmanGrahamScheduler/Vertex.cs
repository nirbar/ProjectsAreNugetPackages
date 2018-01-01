using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffmanGrahamScheduler
{
    /// <summary>
    /// Grapg vertex. A vertex may contain dependencies on other vertices.
    /// </summary>
    public class Vertex
    {
        private List<Vertex> dependencies_ = new List<Vertex>();
        /// <summary>
        /// List of all dependencies. May contain some, all, or none transitive dependencies
        /// </summary>
        public virtual List<Vertex> Dependencies { get => dependencies_; }

        private List<Vertex> directDependencies_ = new List<Vertex>();
        internal List<Vertex> DirectDependencies { get => directDependencies_; }

        internal void TransitiveReduct()
        {
            DirectDependencies.Clear();

            /* Worst case algorithm would be:
            foreach (Vertex vi in Dependencies)
            {
                int middleMan = Dependencies.FindIndex((vj) => ((vi != vj) && vj.Dependencies.Contains(vi)));
                if (middleMan < 0)
                {
                    DirectDependencies.Add(vi);
                }
            }
            */

            // Improved search algo:
            List<Vertex> indirect = new List<Vertex>();
            for (int i = 0; i < Dependencies.Count; ++i)
            {
                Vertex vi = Dependencies[i];
                if (!indirect.Contains(vi))
                {
                    bool iDirect = true;
                    for (int j = i + 1; j < Dependencies.Count; ++j)
                    {
                        Vertex vj = Dependencies[j];
                        if (vi.Dependencies.Contains(vj))
                        {
                            indirect.Add(vj);
                        }
                        else if (vj.Dependencies.Contains(vi))
                        {
                            iDirect = false;
                            break;
                        }
                    }

                    if (iDirect)
                    {
                        DirectDependencies.Add(vi);
                    }
                }
            }
        }

        private List<Vertex> dependants_ = new List<Vertex>();
        internal List<Vertex> DirectDependants { get => dependants_; }

        internal void ResolveDependants(HashSet<Vertex> allVertices)
        {
            DirectDependants.Clear();

            foreach(Vertex v in allVertices)
            {
                if ((v != this) && v.DirectDependencies.Contains(this))
                {
                    DirectDependants.Add(v);
                }
            }
        }
    }
}
