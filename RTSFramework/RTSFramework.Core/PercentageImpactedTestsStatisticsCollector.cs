using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Core
{
	public class PercentageImpactedTestsStatisticsCollector<TTestCase, TDelta, TModel> : ITestProcessor<TTestCase, PercentageImpactedTestsStatistic, TDelta, TModel> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		public Task<PercentageImpactedTestsStatistic> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<ISet<TTestCase>, TTestCase> testsDelta, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			double percentage = (double) impactedTests.Count / testsDelta.NewModel.Count;

			string deltaIdentifier = $"Delta: {impactedForDelta.OldModel.VersionId} --> {impactedForDelta.NewModel.VersionId}";

			var statistcs = new PercentageImpactedTestsStatistic();
			statistcs.DeltaIdPercentageTestsTuples.Add(new Tuple<string, double>(deltaIdentifier, percentage));

			return Task.FromResult(statistcs);
		}
	}
}