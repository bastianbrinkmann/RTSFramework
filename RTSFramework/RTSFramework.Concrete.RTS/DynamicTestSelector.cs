using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicTestSelector<TProgram,TProgramDelta, TTestCase> : ITestSelector<TProgram, TProgramDelta, TTestCase>
		where TTestCase : class, ITestCase
		where TProgram : IProgramModel
		where TProgramDelta : IDelta<TProgram>
	{
		private readonly CorrespondenceModelManager<CSharpClassesProgramModel> correspondenceModelProvider;
		private readonly IDeltaAdapter<TProgramDelta, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, TProgram, CSharpClassesProgramModel> deltaAdapter;

		public DynamicTestSelector(CorrespondenceModelManager<CSharpClassesProgramModel> correspondenceModelProvider,
			IDeltaAdapter<TProgramDelta, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, TProgram, CSharpClassesProgramModel> deltaAdapter)
		{
			this.correspondenceModelProvider = correspondenceModelProvider;
			this.deltaAdapter = deltaAdapter;
		}

		public Task SelectTests(StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta,
			CancellationToken cancellationToken)
		{
			var delta = deltaAdapter.Convert(programDelta);

			CorrespondenceModel = correspondenceModelProvider.GetCorrespondenceModel(delta.OldModel, testsDelta.OldModel);

			ISet<TTestCase> impactedTests = new HashSet<TTestCase>();

			foreach (var testcase in testsDelta.NewModel.TestSuite)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (CorrespondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
				{
					if (delta.ChangedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))) ||
						delta.DeletedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))))
					{
						impactedTests.Add(testcase);
					}
				}
				else
				{
					impactedTests.Add(testcase);
				}
			}

			SelectedTests = impactedTests;

			return Task.CompletedTask;
		}

		public ISet<TTestCase> SelectedTests { get; private set; }
		public ICorrespondenceModel CorrespondenceModel { get; private set; }
	}
}