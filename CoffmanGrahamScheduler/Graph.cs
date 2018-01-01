using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffmanGrahamScheduler
{
    public class Graph
    {
        /// <summary>
        /// List of vertices lists.
        /// </summary>
        public List<HashSet<Vertex>> Levels { get; private set; }

        /// <summary>
        /// Create a Coffman-Graham grpah.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="w">Maximal number of vertices that can be placed in the same level (i.e. number of machine cores in task scheduling application).</param>
        public static Graph CoffmanGraham(HashSet<Vertex> vertices, int w)
        {
            /* Phase 1: 
             * Represent the partial order by its transitive reduction or covering relation, a directed acyclic graph G that has an edge from x to y whenever x < y 
             * and there does not exist any third element z of the partial order for which x < z < y. In the graph drawing applications of the Coffman–Graham algorithm, 
             * the resulting directed acyclic graph may not be the same as the graph being drawn, and in the scheduling applications it may not have an edge for every 
             * precedence constraint of the input: in both cases, the transitive reduction removes redundant edges that are not necessary for defining the partial order.
             */
            foreach (Vertex v in vertices)
            {
                v.TransitiveReduct();
            }
            foreach (Vertex v in vertices)
            {
                v.ResolveDependants(vertices);
            }

            /* Phase 2: 
             * Construct a topological ordering of G in which the vertices are ordered lexicographically by the set of positions of their incoming neighbors. 
             * To do so, add the vertices one at a time to the ordering, at each step choosing a vertex v to add such that the incoming neighbors of v are all already
             * part of the partial ordering, and such that the most recently added incoming neighbor of v is earlier than the most recently added incoming neighbor of any 
             * other vertex that could be added in place of v. If two vertices have the same most recently added incoming neighbor, the algorithm breaks the tie in favor 
             * of the one whose second most recently added incoming neighbor is earlier, etc.
             */
            List<Vertex> topoligicalOrder = new List<Vertex>();
            for (int i = vertices.Count - 1; i >= 0; --i)
            {
                if (topoligicalOrder.Count == vertices.Count)
                {
                    break;
                }

                foreach (Vertex vi in vertices)
                {
                    if (!topoligicalOrder.Contains(vi) && !vi.DirectDependencies.Any((v) => !topoligicalOrder.Contains(v)))
                    {
                        topoligicalOrder.Add(vi);
                    }
                }
            }

            if (topoligicalOrder.Count != vertices.Count)
            {
                throw new ArgumentOutOfRangeException("Some dependencies could not be resolved");
            }

            /* Phase 3:
             * Assign the vertices of G to levels in the reverse of the topological ordering constructed in the previous step. For each vertex v, add v to a level 
             * that is at least one step higher than the highest level of any outgoing neighbor of v, that does not already have W elements assigned to it, 
             * and that is as low as possible subject to these two constraints.
             */
            List<HashSet<Vertex>> g = new List<HashSet<Vertex>>();
            while (topoligicalOrder.Count > 0)
            {
                Vertex v = topoligicalOrder.Last();

                int maxDL = -1;
                foreach (Vertex d in v.DirectDependants)
                {
                    int dl = g.FindIndex((HashSet<Vertex> l) => l.Contains(d));
                    if (dl < 0)
                    {
                        throw new Exception("Topological order error: Dependant sorted before dependencie");
                    }
                    maxDL = Math.Max(maxDL, dl);
                }

                HashSet<Vertex> level = null;
                if (maxDL < g.Count - 1)
                {
                    int li = g.FindIndex(maxDL + 1, (l) => (l.Count < w));
                    if (li >= 0)
                    {
                        level = g[li];
                    }
                }
                if (level == null)
                {
                    level = new HashSet<Vertex>();
                    g.Add(level);
                }

                level.Add(v);
                topoligicalOrder.Remove(v);
            }

            g.Reverse();
            return new Graph() { Levels = g };
        }
    }
}
