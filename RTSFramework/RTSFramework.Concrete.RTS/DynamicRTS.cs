using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicRTS<TModel, TModelElement, TTestCase>
		where TTestCase : class, ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		public ISet<TTestCase> SelectedTests { get; private set; }
		public Func<string, IList<string>> GetResponsibleChangesByTestId { get; private set; }

		public Task SelectTests(ISet<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta, CorrespondenceModel.Models.CorrespondenceModel correspondenceModel, CancellationToken cancellationToken)
		{
			ISet<TTestCase> impactedTests = new HashSet<TTestCase>();

			foreach (var testcase in testCases)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (correspondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
				{
					if (delta.ChangedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))) ||
						delta.DeletedElements.Any(x => linkedElements.Any(y => x.Id.Equals(y, StringComparison.Ordinal))))
					{
						impactedTests.Add(testcase);
					}
				}
				else
				{
					//Unknown testcase - considered as new testcase so impacted
					impactedTests.Add(testcase);
				}
			}

			GetResponsibleChangesByTestId = id =>
			{
				if (correspondenceModel.CorrespondenceModelLinks.ContainsKey(id))
				{
					var linksOfTestcase = correspondenceModel.CorrespondenceModelLinks[id];
					return linksOfTestcase.Where(x => delta.AddedElements.Any(y => y.Id == x) ||
													  delta.ChangedElements.Any(y => y.Id == x) ||
													  delta.DeletedElements.Any(y => y.Id == x)).ToList();
				}

				return new List<string>(new[] { "New Test" });
			};
			SelectedTests = impactedTests;
			return Task.CompletedTask;
		}
	}
}