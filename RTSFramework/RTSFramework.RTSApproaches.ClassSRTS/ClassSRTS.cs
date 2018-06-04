using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class ClassSRTS<TModel, TTestCase> : IStaticRTS<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase, IntertypeRelationGraph> where TTestCase : ITestCase where TModel : IProgramModel
	{
		public Func<string, IList<string>> GetResponsibleChangesByTestId => null;

		public ISet<TTestCase> SelectTests(IntertypeRelationGraph dataStructure, ISet<TTestCase> allTests, StructuralDelta<TModel, CSharpClassElement> delta, CancellationToken cancellationToken)
		{
			ISet<TTestCase> impactedTests = new HashSet<TTestCase>();

			var changedTypes = new List<string>();

			changedTypes.AddRange(delta.AddedElements.Select(x => x.Id));
			changedTypes.AddRange(delta.ChangedElements.Select(x => x.Id));

			var affectedTypes = new List<string>(changedTypes);

			foreach (var type in changedTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ExtendAffectedTypesAndReportImpactedTests(type, dataStructure, affectedTypes, allTests, impactedTests, cancellationToken);
			}

			return impactedTests;
		}

		private void ExtendAffectedTypesAndReportImpactedTests(string type, IntertypeRelationGraph graph, List<string> affectedTypes, ISet<TTestCase> allTests, ISet<TTestCase> impactedTests, CancellationToken cancellationToken)
		{
			foreach (var test in allTests.Where(x => x.AssociatedClass == type))
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
	}
}