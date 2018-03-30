using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    /// <summary>
    /// An extensive study of static regression test selection in modern software evolution
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// </summary>
    public class ClassSRTSApproach<TP> : RTSApproachBase<TP, CSharpClassElement, MSTestTestcase> where TP : ICSharpProgramModel
    {
        private readonly IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter;
        private readonly IntertypeRelationGraphBuilder intertypeRelationGraphBuilder;

        public ClassSRTSApproach(IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter,
            IntertypeRelationGraphBuilder intertypeRelationGraphBuilder)
        {
            this.assembliesArtefactAdapter = assembliesArtefactAdapter;
            this.intertypeRelationGraphBuilder = intertypeRelationGraphBuilder;
        }

        public override void ExecuteRTS(IEnumerable<MSTestTestcase> testCases, StructuralDelta<TP, CSharpClassElement> delta)
        {
            var assemblies = assembliesArtefactAdapter.Parse(delta.SourceModel.AbsoluteSolutionPath);

            IntertypeRelationGraph graph = intertypeRelationGraphBuilder.BuildIntertypeRelationGraph(assemblies);
        }
    }
}