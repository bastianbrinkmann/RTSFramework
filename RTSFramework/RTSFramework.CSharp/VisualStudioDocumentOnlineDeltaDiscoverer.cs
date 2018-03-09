using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp
{
    public class VisualStudioDocumentOnlineDeltaDiscoverer : IOnlineDeltaDiscoverer<CSharpProgram, CSharpDocument, IDelta<CSharpDocument>>
    {

        private OperationalDelta<CSharpDocument> delta = new OperationalDelta<CSharpDocument>();

        private Solution solution;
        private MSBuildWorkspace workspace;


        public IDelta<CSharpDocument> GetCurrentDelta()
        {
            return delta;
        }

        public void StopDiscovery()
        {
            workspace.WorkspaceChanged -= MSWorkspaceOnWorkspaceChanged;
            solution = null;
            workspace.CloseSolution();
        }
        
        public void StartDiscovery(CSharpProgram startingVersion)
        {
            var msWorkspace = MSBuildWorkspace.Create();
            solution = msWorkspace.OpenSolutionAsync(startingVersion.SolutionPath).Result;
            msWorkspace.WorkspaceChanged += MSWorkspaceOnWorkspaceChanged;
        }

        private void MSWorkspaceOnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            if (args.Kind == WorkspaceChangeKind.DocumentChanged)
            {
                var rosylnDocument = solution.GetDocument(args.DocumentId);
                var csharpDocument = new CSharpDocument(args.DocumentId.ToString());

                delta.ChangedElements.Add(csharpDocument);
            }
        }
    }
}