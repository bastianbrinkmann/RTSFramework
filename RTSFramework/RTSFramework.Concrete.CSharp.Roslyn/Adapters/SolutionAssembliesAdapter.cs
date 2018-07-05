using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.Roslyn.Adapters
{
	public class SolutionAssembliesAdapter : CancelableArtefactAdapter<string, IList<CSharpAssembly>>
	{
		private readonly ISettingsProvider settingsProvider;

		public SolutionAssembliesAdapter(ISettingsProvider settingsProvider)
		{
			this.settingsProvider = settingsProvider;
		}

		public override async Task<IList<CSharpAssembly>> Parse(string artefact, CancellationToken token)
		{
			var result = new List<CSharpAssembly>();

			var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			{
				{ "Configuration", settingsProvider.Configuration },
				{ "Platform", settingsProvider.Platform }
			});

			var solution = await workspace.OpenSolutionAsync(artefact, token);

			foreach (var project in solution.Projects)
			{
				if (!File.Exists(project.OutputFilePath))
				{
					throw new ArgumentException($"The assembly {project.OutputFilePath} for project {project.Name} does not exist!" +
												$"\nCheck the configuration and platform configured in the app settings (Configuration: {settingsProvider.Configuration}, Platform: {settingsProvider.Platform}).");
				}

				VersionStamp latestDocumentVersion = await project.GetLatestDocumentVersionAsync(token);

				var assemblyCreationTime = File.GetLastWriteTimeUtc(project.OutputFilePath);
				
				var assemblyVersionStamp = VersionStamp.Create(assemblyCreationTime);

				var newerTimeStamp = latestDocumentVersion.GetNewerVersion(assemblyVersionStamp);
				if (newerTimeStamp != assemblyVersionStamp)
				{
					throw new ArgumentException($"The assembly {project.OutputFilePath} for project {project.Name} is not up-to-date, please compile the project first!");
				}

				token.ThrowIfCancellationRequested();
				result.Add(new CSharpAssembly { AbsolutePath = project.OutputFilePath });
			}

			return result;
		}

		public override Task<string> Unparse(IList<CSharpAssembly> model, string artefact, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}
}