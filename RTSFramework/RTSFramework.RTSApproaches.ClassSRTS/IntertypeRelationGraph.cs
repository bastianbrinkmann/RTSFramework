using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    /// <summary>
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// Definition IRG (intertype relation graph):
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
        public HashSet<IntertypeRelationGraphNode> Nodes { get; } = new HashSet<IntertypeRelationGraphNode>();

        public HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>> InheritanceEdges { get; } = 
            new HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>>();

        public HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>> UseEdges { get; } =
            new HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>>();

        public void AddInheritanceEdgeIfBothExist(TypeDefinition from, TypeReference to)
        {
            AddEdgeIfBothExist(from, to, InheritanceEdges);
        }

        public void AddUseEdgeIfBothExist(TypeDefinition from, TypeReference to)
        {
            AddEdgeIfBothExist(from, to, UseEdges);
        }

        private void AddEdgeIfBothExist(TypeDefinition from, TypeReference to, HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>> edges)
        {
            var genericType = to as GenericInstanceType;
            if (genericType != null && genericType.HasGenericArguments)
            {
                foreach (var genericArgument in genericType.GenericArguments)
                {
                    AddEdgeIfBothExist(from, genericArgument, edges);
                }
            }

            var fromNode = Nodes.SingleOrDefault(x => x.TypeIdentifier == from.FullName);
            var toNode = Nodes.SingleOrDefault(x => x.TypeIdentifier == to.FullName);

            if (fromNode != null && toNode != null &&
                !edges.Any(x => x.Item1.TypeIdentifier == from.FullName && x.Item2.TypeIdentifier == to.FullName))
            {
                edges.Add(new Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>(fromNode, toNode));
            }
        }
    }
}