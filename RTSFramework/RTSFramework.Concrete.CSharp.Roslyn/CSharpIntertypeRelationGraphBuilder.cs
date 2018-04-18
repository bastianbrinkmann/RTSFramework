using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
	public class CSharpIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureProvider<IntertypeRelationGraph, TCSharpModel> where TCSharpModel : CSharpProgramModel
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

			foreach (var project in solution.Projects)
			{
				token.ThrowIfCancellationRequested();

				var compilation = await project.GetCompilationAsync(token);

				foreach (var namespaceSymbol in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
				{
					AddNodeIfNotExists(namespaceSymbol, graph);
				}
			}

			return graph;
		}

		private void AddNodeIfNotExists(INamespaceOrTypeSymbol namedType, IntertypeRelationGraph graph)
		{
			if (namedType == null)
			{
				return;
			}

			if (namedType.IsType)
			{
				var typeName = namedType.ToDisplayString();

				if (graph.Nodes.All(x => x.TypeIdentifier != typeName))
				{
					graph.Nodes.Add(new IntertypeRelationGraphNode(typeName));
				}
			}
			else
			{
				foreach (var type in namedType.GetMembers())
				{
					AddNodeIfNotExists(type as INamespaceOrTypeSymbol, graph);
				}
			}
		}

		public Task PersistDataStructure(IntertypeRelationGraph dataStructure)
		{
			throw new NotImplementedException();
		}
	}
}