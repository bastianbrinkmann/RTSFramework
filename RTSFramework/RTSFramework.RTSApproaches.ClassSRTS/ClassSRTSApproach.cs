using System.Collections.Generic;
using System.Linq;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    /// <summary>
    /// An extensive study of static regression test selection in modern software evolution
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// </summary>
    public class ClassSRTSApproach<TP> : RTSApproachBase<TP, CSharpClassElement, MSTestTestcase> where TP: IProgramModel
    {
        private readonly IFilesProvider<TP> filesProvider;

        public ClassSRTSApproach(IFilesProvider<TP> filesProvider)
        {
            this.filesProvider = filesProvider;
        }

        public override void ExecuteRTS(IEnumerable<MSTestTestcase> testCases, StructuralDelta<TP, CSharpClassElement> delta)
        {
            var allFiles = filesProvider.GetAllFiles(delta.SourceModel);

            foreach (var files in allFiles.Where(x => x.EndsWith(".cs")))
            {
                
            }
        }
        /// <summary>
        /// https://dl.acm.org/citation.cfm?id=2950361
        /// Definition IRG (intertype relation graph):
        /// An intertype relation graph, IRG, of a given program is a triple (N,EI,EU) where: 
        /// • N is the set of nodes representing all types in the program; 
        /// • EI ⊆ N × N is the set of inheritance edges; 
        /// there exists an edge (n1,n2) ∈ EI if type n1 inherits from n2,
        /// and a class implementing an interface is in the inheritance relation;
        /// • EU ⊆ N × N is the set of use edges; there exists an edge (n1,n2) ∈ EU if type n1 directly references n2, 
        /// and aggregations and associations are in the use relations.
        /// </summary>
        private void BuildIntertypeRelationGraph()
        {
            //TODO: Build via syntax tree or reflection?   
        }
    }
}