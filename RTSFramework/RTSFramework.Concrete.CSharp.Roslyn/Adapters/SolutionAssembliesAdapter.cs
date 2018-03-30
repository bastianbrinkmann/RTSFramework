using System.Collections.Generic;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.Roslyn.Adapters
{
    public class SolutionAssembliesAdapter : IArtefactAdapter<string, IList<CSharpAssembly>>
    {
        public IList<CSharpAssembly> Parse(string artefact)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(artefact).Result;
            var result = new List<CSharpAssembly>();

            foreach (var project in solution.Projects)
            {
                
            }

            return result;
        }

        public void Unparse(IList<CSharpAssembly> model, string artefact)
        {
            throw new System.NotImplementedException();
        }
    }
}