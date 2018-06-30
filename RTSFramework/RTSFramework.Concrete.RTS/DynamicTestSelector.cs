using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicTestSelector<TModel, TModelElement, TTestCase> : ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>
		where TTestCase : class, ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		private readonly CorrespondenceModelManager<TModel> correspondenceModelProvider;

		public DynamicTestSelector(CorrespondenceModelManager<TModel> correspondenceModelProvider)
		{
			this.correspondenceModelProvider = correspondenceModelProvider;
		}

		public Task SelectTests(StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, StructuralDelta<TModel, TModelElement> delta,
			CancellationToken cancellationToken)
		{
			CorrespondenceModel = correspondenceModelProvider.GetCorrespondenceModel(delta.OldModel);

			ISet<TTestCase> impactedTests = new HashSet<TTestCase>();

			foreach (var testcase in testsDelta.NewModel)
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