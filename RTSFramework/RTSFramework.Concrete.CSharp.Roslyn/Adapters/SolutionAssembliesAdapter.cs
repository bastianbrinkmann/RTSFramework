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

			var workspace = MSBuildWorkspace.Create();
	        Solution solution;

			try
	        {
				solution = await workspace.OpenSolutionAsync(artefact, token);
			}
			catch (OperationCanceledException)
	        {
		        return result;
	        }

            foreach (var project in solution.Projects)
            {
                //TODO Check somehow whether project needs to be rebuilt?
	            if (token.IsCancellationRequested)
	            {
		            return result;
	            }

                result.Add(new CSharpAssembly{AbsolutePath = project.OutputFilePath});
            }

            return result;
        }

        public override Task Unparse(IList<CSharpAssembly> model, string artefact, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}