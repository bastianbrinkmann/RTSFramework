using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.Roslyn.Adapters
{
	public class SolutionAssembliesAdapter : CancelableArtefactAdapter<string, IList<CSharpAssembly>>
	{
		public override async Task<IList<CSharpAssembly>> Parse(string artefact, CancellationToken token)
		{
			var result = new List<CSharpAssembly>();

			//TODO App Settings
			var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			{
				{ "Configuration", "net_3_5_Debug_ReadOnly" },//{ "Configuration", "Debug" },
				{ "Platform", "Any CPU" }
			});

			var solution = await workspace.OpenSolutionAsync(artefact, token);

			foreach (var project in solution.Projects)
			{
				//TODO Check somehow whether project needs to be rebuilt?
				token.ThrowIfCancellationRequested();
				result.Add(new CSharpAssembly { AbsolutePath = project.OutputFilePath });
			}

			return result;
		}

		public override Task Unparse(IList<CSharpAssembly> model, string artefact, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}
}