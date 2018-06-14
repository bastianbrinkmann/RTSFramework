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
		public Task<PercentageImpactedTestsStatistic> ProcessTests(IList<TTestCase> impactedTests, ISet<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			double percentage = (double) impactedTests.Count / allTests.Count;

			string deltaIdentifier = $"Delta: {impactedForDelta.OldModel.VersionId} --> {impactedForDelta.NewModel.VersionId}";

			return Task.FromResult(new PercentageImpactedTestsStatistic
			{
				PercentageImpactedTests = percentage,
				DeltaIdentifier = deltaIdentifier
			});
		}
	}
}