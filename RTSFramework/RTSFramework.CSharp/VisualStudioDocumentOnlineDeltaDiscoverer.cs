using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Contracts.DeltaDiscoverer;

namespace RTSFramework.Concrete.CSharp
{
    //TODO Remove?
    public class VisualStudioDocumentOnlineDeltaDiscoverer : IOnlineDeltaDiscoverer<CSharpProgramModel, IDelta>
    {

        private StructuralDelta<CSharpFileElement> delta = new StructuralDelta<CSharpFileElement>();

        private Solution solution;
        private MSBuildWorkspace workspace;


        public IDelta GetCurrentDelta()
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
            delta.SourceModelId = startingVersion.VersionId;
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