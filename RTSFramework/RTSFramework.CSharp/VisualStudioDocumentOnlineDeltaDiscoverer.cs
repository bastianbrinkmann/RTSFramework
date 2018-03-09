using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp
{
    public class VisualStudioDocumentOnlineDeltaDiscoverer : IOnlineDeltaDiscoverer<CSharpProgram, CSharpDocument, IDelta<CSharpDocument, CSharpProgram>>
    {

        private OperationalDelta<CSharpDocument, CSharpProgram> delta = new OperationalDelta<CSharpDocument, CSharpProgram>();

        private Solution solution;
        private MSBuildWorkspace workspace;


        public IDelta<CSharpDocument, CSharpProgram> GetCurrentDelta()
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
            delta.Source = startingVersion;
            workspace = MSBuildWorkspace.Create();
            solution = workspace.OpenSolutionAsync(startingVersion.SolutionPath).Result;
            workspace.WorkspaceChanged += MSWorkspaceOnWorkspaceChanged;
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