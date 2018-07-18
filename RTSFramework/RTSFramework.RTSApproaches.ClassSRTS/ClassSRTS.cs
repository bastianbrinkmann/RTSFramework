using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class ClassSRTS<TProgram, TTestCase> : IStaticRTS<TProgram, StructuralDelta<TProgram, CSharpClassElement>, TTestCase, IntertypeRelationGraph> where TTestCase : ITestCase where TProgram : IProgramModel
	{
		public ISet<TTestCase> SelectTests(IntertypeRelationGraph dataStructure, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, StructuralDelta<TProgram, CSharpClassElement> programDelta, CancellationToken cancellationToken)
		{
			ISet<ImpactedTest<TTestCase>> impactedTests = new HashSet<ImpactedTest<TTestCase>>();

			var changedTypes = new List<AffectedType>();

			changedTypes.AddRange(programDelta.AddedElements.Select(x => new AffectedType {Id = x.Id, ImpactedDueTo = x.Id}));
			changedTypes.AddRange(programDelta.ChangedElements.Select(x => new AffectedType { Id = x.Id, ImpactedDueTo = x.Id }));

			var affectedTypes = new List<AffectedType>(changedTypes);

			foreach (var type in changedTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ExtendAffectedTypesAndReportImpactedTests(type, dataStructure, affectedTypes, testsDelta.NewModel.TestSuite, impactedTests, cancellationToken);
			}

			testsDelta.AddedElements.ForEach(x =>
			{
				if (impactedTests.All(y => y.TestCase.Id != x.Id))
				{
					impactedTests.Add(new ImpactedTest<TTestCase> {TestCase = x, ImpactedDueTo = null});
				}
			});

			CorrespondenceModel = new CorrespondenceModel.Models.CorrespondenceModel
			{
				ProgramVersionId = programDelta.NewModel.VersionId,
				CorrespondenceModelLinks = impactedTests.ToDictionary(
					x => x.TestCase.Id, 
					x => x.ImpactedDueTo == null ? new HashSet<string>() : new HashSet<string>(new[] {x.ImpactedDueTo}))
			};

			return new HashSet<TTestCase>(impactedTests.Select(x => x.TestCase));
		}

		private void ExtendAffectedTypesAndReportImpactedTests(AffectedType type, IntertypeRelationGraph graph, List<AffectedType> affectedTypes, ISet<TTestCase> allTests, ISet<ImpactedTest<TTestCase>> impactedTests, CancellationToken cancellationToken)
		{
			foreach (var test in allTests.Where(x => x.AssociatedClasses.Contains(type.Id)))
			{
				if (impactedTests.All(y => y.TestCase.Id != test.Id))
				{
					impactedTests.Add(new ImpactedTest<TTestCase> { TestCase = test, ImpactedDueTo = type.ImpactedDueTo });
				}
			}

			var usedByTypes = graph.UseEdges.Where(x => x.Item2 == type.Id).Select(x => x.Item1);

			foreach (string usedByType in usedByTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (affectedTypes.All(x => x.Id != usedByType))
				{
					var newAffectedType = new AffectedType {Id = usedByType, ImpactedDueTo = type.ImpactedDueTo};
					affectedTypes.Add(newAffectedType);
					ExtendAffectedTypesAndReportImpactedTests(newAffectedType, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}

			// https://dl.acm.org/citation.cfm?id=2950361 
			// Section 2.1 Class-Level Static RTS (ClassSRTS)
			// "Note that ClassSRTS need not include supertypes of the changed types (but must include all subtypes) 
			// in the transitive closure because a test cannot be affected statically by the changes even if the 
			// test reaches supertype(s) of the changed types unless the test also reaches a changed type or (one of) its subtypes."
			var subTypes = graph.InheritanceEdges.Where(x => x.Item2 == type.Id).Select(x => x.Item1);

			foreach (string subtype in subTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (affectedTypes.All(x => x.Id != subtype))
				{
					var newAffectedType = new AffectedType { Id = subtype, ImpactedDueTo = type.ImpactedDueTo };
					affectedTypes.Add(newAffectedType);
					ExtendAffectedTypesAndReportImpactedTests(newAffectedType, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}

			//However, this might not be true if dependency injection is used:
			//Instances of objects are injected dynamically which happens out of scope of the static analysis
			//-> Pessimistic approach selects also all super types
			var superTypes = graph.InheritanceEdges.Where(x => x.Item1 == type.Id).Select(x => x.Item2);

			foreach (string superType in superTypes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (affectedTypes.All(x => x.Id != superType))
				{
					var newAffectedType = new AffectedType { Id = superType, ImpactedDueTo = type.ImpactedDueTo };
					affectedTypes.Add(newAffectedType);
					ExtendAffectedTypesAndReportImpactedTests(newAffectedType, graph, affectedTypes, allTests, impactedTests, cancellationToken);
				}
			}
		}

		public ICorrespondenceModel CorrespondenceModel { get; private set; }
	}
}