using System;
using System.Collections.Generic;
using System.Linq;

namespace RTSFramework.RTSApproaches.Core.DataStructures
{
    /// <summary>
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// Definition 2.1 Intertype Relation Graph (IRG):
    /// An intertype relation graph, IRG, of a given program is a triple (N,EI,EU) where: 
    /// • N is the set of nodes representing all types in the program; 
    /// • EI ⊆ N × N is the set of inheritance edges; 
    /// there exists an edge (n1,n2) ∈ EI if type n1 inherits from n2,
    /// and a class implementing an interface is in the inheritance relation;
    /// • EU ⊆ N × N is the set of use edges; there exists an edge (n1,n2) ∈ EU if type n1 directly references n2, 
    /// and aggregations and associations are in the use relations.
    /// </summary>
    public class IntertypeRelationGraph
    {
        public HashSet<string> Nodes { get; } = new HashSet<string>();

		public void AddInheritanceEdgeIfNotExists(string from, string to)
		{
			lock (InheritanceEdges)
			{
				if (!InheritanceEdges.Any(x => x.Item1 == from && x.Item2 == to))
				{
					InheritanceEdges.Add(new Tuple<string, string>(from, to));
				}
			}
		}

		public void AddUseEdgeIfNotExists(string from, string to)
		{
			lock (UseEdges)
			{
				if (!UseEdges.Any(x => x.Item1 == from && x.Item2 == to))
				{
					UseEdges.Add(new Tuple<string, string>(from, to));
				}
			}
		}

		public HashSet<Tuple<string, string>> InheritanceEdges { get; } = new HashSet<Tuple<string, string>>();

        public HashSet<Tuple<string, string>> UseEdges { get; } = new HashSet<Tuple<string, string>>();

        
    }
}