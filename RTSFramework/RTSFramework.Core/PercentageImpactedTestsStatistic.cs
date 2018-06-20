using System;
using System.Collections.Generic;
using RTSFramework.Contracts;

namespace RTSFramework.Core
{
	public class PercentageImpactedTestsStatistic : ITestProcessingResult
	{
		public List<Tuple<string, double>> DeltaIdPercentageTestsTuples { get; } = new List<Tuple<string, double>>();
		
	}
}