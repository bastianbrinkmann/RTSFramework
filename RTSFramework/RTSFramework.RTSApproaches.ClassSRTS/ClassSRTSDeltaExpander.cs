using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	/// <summary>
	/// https://dl.acm.org/citation.cfm?id=2950361
	/// An extensive study of static regression test selection in modern software evolution
	/// by Legunsen et al.
	/// 
	/// Called Delta Expansion as in:
	/// https://link.springer.com/chapter/10.1007/978-3-642-34026-0_9
	/// A Generic Platform for Model-Based Regression Testing
	/// by Zech et al.
	/// </summary>
	public class ClassSRTSDeltaExpander<TModel> : ITestSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, MSTestTestcase>
		where TModel : CSharpProgramModel 
	{
		private readonly IDataStructureProvider<IntertypeRelationGraph, TModel> irgBuilder;

		public ClassSRTSDeltaExpander(IDataStructureProvider<IntertypeRelationGraph, TModel> irgBuilder)
		{
			this.irgBuilder = irgBuilder;
		}

		public async Task<IList<MSTestTestcase>>  SelectTests(IList<MSTestTestcase> testCases, StructuralDelta<TModel, CSharpClassElement> delta, CancellationToken cancellationToken)
		{
			//Using the IRG for P' is possible as it is built using the intermediate language
			//Therefore, the program at least compiles - preventing issues from for example deleted files
			var graph = await irgBuilder.GetDataStructureForProgram(delta.NewModel, cancellationToken);

			return ExpandDelta(graph, testCases, delta, cancellationToken);
		}

		/// <summary>
		/// Called Delta Expansion as in:
		/// https://link.springer.com/chapter/10.1007/978-3-642-34026-0_9
		/// A Generic Platform for Model-Based Regression Testing
		/// by Zech et al.
		/// </summary>
		private IList<MSTestTestcase> ExpandDelta(IntertypeRelationGraph graph, IList<MSTestTestcase> allTests, StructuralDelta<TModel, CSharpClassElement> delta, CancellationToken cancellationToken)
		{
			IList<MSTestTestcase> impactedTests = new List<MSTestTestcase>();

			var changedTypes = new List<string>();

			changedTypes.AddRange(delta.AddedElements.Select(x => x.Id));
			changedTypes.AddRange(delta.ChangedElements.Select(x => x.Id));

			var affectedTypes = new List<string>(changedTypes);

			foreach (var type in changedTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ExtendAffectedTypesAndReportImpactedTests(type, graph, affectedTypes, allTests, impactedTests, cancellationToken);
			}

			return impactedTests;
		}

		private void ExtendAffectedTypesAndReportImpactedTests(string type, IntertypeRelationGraph graph, List<string> affectedTypes, IList<MSTestTestcase> allTests, IList<MSTestTestcase> impactedTests, CancellationToken cancellationToken)
		{
			foreach (var test in allTests.Where(x => x.FullClassName == type))
			{
				impactedTests.Add(test);
			}

			var usedByTypes = graph.UseEdges.Where(x => x.Item2 == type).Select(x => x.Item1);

			foreach (string usedByType in usedByTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!affectedTypes.Contains(usedByType))
				{
					affectedTypes.Add(usedByType);
					ExtendAffectedTypesAndReportImpactedTests(usedByType, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}

			// https://dl.acm.org/citation.cfm?id=2950361 
			// Section 2.1 Class-Level Static RTS (ClassSRTS)
			// "Note that ClassSRTS need not include supertypes of the changed types (but must include all subtypes) 
			// in the transitive closure because a test cannot be affected statically by the changes even if the 
			// test reaches supertype(s) of the changed types unless the test also reaches a changed type or (one of) its subtypes."
			var subTypes = graph.InheritanceEdges.Where(x => x.Item2 == type).Select(x => x.Item1);

			foreach (string subtype in subTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!affectedTypes.Contains(subtype))
				{
					affectedTypes.Add(subtype);
					ExtendAffectedTypesAndReportImpactedTests(subtype, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}

			//However, this might not be true if dependency injection is used:
			//Instances of objects are injected dynamically which happens out of scope of the static analysis
			//-> Pessimistic approach selects also all super types
			var superTypes = graph.InheritanceEdges.Where(x => x.Item1 == type).Select(x => x.Item2);

			foreach (string superType in superTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!affectedTypes.Contains(superType))
				{
					affectedTypes.Add(superType);
					ExtendAffectedTypesAndReportImpactedTests(superType, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}
		}

		public IResponsibleChangesProvider GetResponsibleChangesProvider()
		{
			return null;
		}
	}
}