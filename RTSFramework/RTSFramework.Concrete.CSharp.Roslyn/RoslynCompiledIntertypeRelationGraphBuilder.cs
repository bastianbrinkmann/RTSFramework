using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
	public class RoslynCompiledIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureProvider<IntertypeRelationGraph, TCSharpModel> where TCSharpModel : CSharpProgramModel
	{
		public async Task<IntertypeRelationGraph> GetDataStructureForProgram(TCSharpModel sourceModel, CancellationToken token)
		{
			var graph = new IntertypeRelationGraph();
			var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			{
				//{ "Configuration", "Debug" },//{ "Configuration", "net_3_5_Debug_ReadOnly" },
				//{ "Platform", "Any CPU" }
			});

			var solution = await workspace.OpenSolutionAsync(sourceModel.AbsoluteSolutionPath, token);

			List<Compilation> compilations = new List<Compilation>();

			var typeSymbols = new List<INamedTypeSymbol>();

			foreach (var project in solution.Projects)
			{
				token.ThrowIfCancellationRequested();

				var compilation = await project.GetCompilationAsync(token);

				compilations.Add(compilation);

				foreach (var namespaceSymbol in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
				{
					AddNodeIfNotExists(namespaceSymbol, graph, typeSymbols);
				}
			}

			foreach (INamedTypeSymbol type in typeSymbols)
			{
				if (type.BaseType != null)
				{
					AddInheritanceEdgeIfBothExist(type, type.BaseType, graph);
				}
			}

			return graph;
		}

		private void AddNodeIfNotExists(INamespaceOrTypeSymbol namedType, IntertypeRelationGraph graph, List<INamedTypeSymbol> typeSymbols)
		{
			if (namedType == null)
			{
				return;
			}

			if (namedType.IsType)
			{
				typeSymbols.Add((INamedTypeSymbol) namedType);

				var typeName = GetTypeIdentifier(namedType);
				if (graph.Nodes.All(x => x.TypeIdentifier != typeName))
				{
					graph.Nodes.Add(new IntertypeRelationGraphNode(typeName));
				}
			}
			else
			{
				foreach (var type in namedType.GetMembers())
				{
					AddNodeIfNotExists(type as INamespaceOrTypeSymbol, graph, typeSymbols);
				}
			}
		}

		private string GetTypeIdentifier(INamespaceOrTypeSymbol symbol)
		{
			if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
			{
				return symbol.MetadataName;
			}
			
			var parentName = GetTypeIdentifier(symbol.ContainingNamespace);

			return parentName + "." + symbol.MetadataName;
		}

		private void AddInheritanceEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, graph.InheritanceEdges, graph);
		}

		private void AddUseEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, graph.UseEdges, graph);
		}

		private void AddEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, HashSet<Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>> edges, IntertypeRelationGraph graph)
		{
			if (from == null || to == null)
				return;

			if (to.IsGenericType)
			{
				foreach (var genericArugment in to.TypeArguments)
				{
					AddUseEdgeIfBothExist(from, genericArugment as INamedTypeSymbol, graph);
				}
			}
			var fromName = GetTypeIdentifier(from);
			var toName = GetTypeIdentifier(to);

			var fromNode = graph.Nodes.SingleOrDefault(x => x.TypeIdentifier == fromName);
			var toNode = graph.Nodes.SingleOrDefault(x => x.TypeIdentifier == toName);

			if (fromNode != null && toNode != null &&
				!edges.Any(x => x.Item1.TypeIdentifier == fromName && x.Item2.TypeIdentifier == toName))
			{
				edges.Add(new Tuple<IntertypeRelationGraphNode, IntertypeRelationGraphNode>(fromNode, toNode));
			}
		}

		public Task PersistDataStructure(IntertypeRelationGraph dataStructure)
		{
			throw new NotImplementedException();
		}
	}
}