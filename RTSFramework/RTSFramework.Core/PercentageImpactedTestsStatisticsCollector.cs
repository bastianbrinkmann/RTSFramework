using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Core
{
	public class PercentageImpactedTestsStatisticsCollector<TTestCase, TProgramDelta, TProgram> : ITestProcessor<TTestCase, PercentageImpactedTestsStatistic, TProgramDelta, TProgram> where TTestCase : ITestCase where TProgramDelta : IDelta<TProgram> where TProgram : IProgramModel
	{
		public Task<PercentageImpactedTestsStatistic> ProcessTests(IList<TTestCase> impactedTests, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, TProgramDelta programDelta, CancellationToken cancellationToken)
		{
			double percentage = (double) impactedTests.Count / testsDelta.NewModel.TestSuite.Count;

			string deltaIdentifier = $"Delta: {programDelta.OldModel.VersionId} --> {programDelta.NewModel.VersionId}";

			var statistcs = new PercentageImpactedTestsStatistic();
			statistcs.DeltaIdPercentageTestsTuples.Add(new Tuple<string, double>(deltaIdentifier, percentage));

			return Task.FromResult(statistcs);
		}
	}
}