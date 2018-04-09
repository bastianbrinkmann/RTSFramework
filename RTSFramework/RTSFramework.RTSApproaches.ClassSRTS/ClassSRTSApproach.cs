using System.Collections.Generic;
using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;
using RTSFramework.Core.Utilities;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    /// <summary>
    /// An extensive study of static regression test selection in modern software evolution
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// </summary>
    public class ClassSRTSApproach<TP> : RTSApproachBase<TP, CSharpClassElement, MSTestTestcase> where TP : CSharpProgramModel
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

            IntertypeRelationGraph graph = null;
            DebugStopWatchTracker.ReportNeededTimeOnDebug(() => graph = intertypeRelationGraphBuilder.BuildIntertypeRelationGraph(assemblies),
                "Building IntertypeRelationGraph");

            var changedTypes = new List<string>();

            changedTypes.AddRange(delta.ChangedElements.Select(x => x.Id));
            changedTypes.AddRange(delta.DeletedElements.Select(x => x.Id));

            var msTestTestcases = testCases as IList<MSTestTestcase> ?? testCases.ToList();

            var affectedTypes = new List<string>(changedTypes);
            DebugStopWatchTracker.ReportNeededTimeOnDebug(() =>
                {
                    foreach (var type in changedTypes)
                    {
                        ExtendAffectedTypesAndReportImpactedTests(type, graph, affectedTypes, msTestTestcases);
                    }
                }, "Extend AffectedTypes and report ImpactedTests");
        }

        private void ReportImpactedTests(string type, IList<MSTestTestcase> testcases)
        {
            var impactedTests = testcases.Where(x => x.FullClassName == type);
            foreach (var impactedTest in impactedTests)
            {
                ReportToAllListeners(impactedTest);
            }
        }

        private void ExtendAffectedTypesAndReportImpactedTests(string type, IntertypeRelationGraph graph, List<string> affectedTypes, IList<MSTestTestcase> testCases)
        {
            ReportImpactedTests(type, testCases);

            var usedByTypes = graph.UseEdges.Where(x => x.Item2.TypeIdentifier == type).Select(x => x.Item1.TypeIdentifier);

            foreach (string usedByType in usedByTypes)
            {
                if (!affectedTypes.Contains(usedByType))
                {
                    affectedTypes.Add(usedByType);
                    ExtendAffectedTypesAndReportImpactedTests(usedByType, graph, affectedTypes, testCases);
                }
            }

            //https://dl.acm.org/citation.cfm?id=2950361 (2.1 Class-Level Static RTS (ClassSRTS))
            //Note that ClassSRTS need not include supertypes of the changed types (but must include all subtypes) 
            //in the transitive closure because a test cannot be affected statically by the changes even if the 
            //test reaches supertype(s) of the changed types unless the test also reaches a changed type or (one of) its subtypes.
            var subTypes = graph.InheritanceEdges.Where(x => x.Item2.TypeIdentifier == type).Select(x => x.Item1.TypeIdentifier);

            foreach (string subtype in subTypes)
            {
                if (!affectedTypes.Contains(subtype))
                {
                    affectedTypes.Add(subtype);
                    ExtendAffectedTypesAndReportImpactedTests(subtype, graph, affectedTypes, testCases);
                }
            }
        }
    }
}