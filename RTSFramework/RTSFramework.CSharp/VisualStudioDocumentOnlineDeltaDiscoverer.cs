using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.CSharp.Core
{
    //TODO Remove?
    public class VisualStudioDocumentOnlineDeltaDiscoverer : IOnlineDeltaDiscoverer<CSharpProgramModel, IDelta<CSharpProgramModel>>
    {

        private StructuralDelta<CSharpProgramModel, CSharpFileElement> delta = new StructuralDelta<CSharpProgramModel, CSharpFileElement>();

        private Solution solution;
        private MSBuildWorkspace workspace;


        public IDelta<CSharpProgramModel> GetCurrentDelta()
        {
            return delta;
        }

        public void StopDiscovery()
        {
            workspace.WorkspaceChanged -= MSWorkspaceOnWorkspaceChanged;
            solution = null;
            workspace.CloseSolution();
        }
        
        public void StartDiscovery(CSharpProgramModel startingVersion)
        {
            delta.SourceModel = startingVersion;
            workspace = MSBuildWorkspace.Create();
            solution = workspace.OpenSolutionAsync(startingVersion.SolutionPath).Result;
            workspace.WorkspaceChanged += MSWorkspaceOnWorkspaceChanged;
        }

        private void MSWorkspaceOnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            if (args.Kind == WorkspaceChangeKind.DocumentChanged)
            {
                var rosylnDocument = solution.GetDocument(args.DocumentId);
                var csharpDocument = new CSharpFileElement(args.DocumentId.ToString());

                delta.ChangedElements.Add(csharpDocument);
            }
        }
    }
}