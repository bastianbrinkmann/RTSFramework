﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.Dynamic
{
	public class DynamicRTS<TModel, TModelElement, TTestCase> : ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>
		where TTestCase : class, ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		private readonly IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> correspondenceModelProvider;

		public DynamicRTS(IDataStructureProvider<CorrespondenceModel.Models.CorrespondenceModel, TModel> correspondenceModelProvider)
		{
			this.correspondenceModelProvider = correspondenceModelProvider;
		}

		public async Task SelectTests(ISet<TTestCase> testCases, StructuralDelta<TModel, TModelElement> delta,
			CancellationToken cancellationToken)
		{
			var currentCorrespondenceModel = await correspondenceModelProvider.GetDataStructure(delta.OldModel, cancellationToken);

			ISet<TTestCase> impactedTests = new HashSet<TTestCase>();

			foreach (var testcase in testCases)
			{
				cancellationToken.ThrowIfCancellationRequested();

				HashSet<string> linkedElements;
				if (currentCorrespondenceModel.CorrespondenceModelLinks.TryGetValue(testcase.Id, out linkedElements))
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
				if (currentCorrespondenceModel.CorrespondenceModelLinks.ContainsKey(id))
				{
					var linksOfTestcase = currentCorrespondenceModel.CorrespondenceModelLinks[id];
					return linksOfTestcase.Where(x => delta.AddedElements.Any(y => y.Id == x) ||
													  delta.ChangedElements.Any(y => y.Id == x) ||
													  delta.DeletedElements.Any(y => y.Id == x)).ToList();
				}

				return new List<string>(new[] { "New Test" });
			};
			SelectedTests = impactedTests;
		}

		public ISet<TTestCase> SelectedTests { get; private set; }
		public Func<string, IList<string>> GetResponsibleChangesByTestId { get; private set; }
	}
}