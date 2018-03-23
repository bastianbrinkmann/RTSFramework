using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Concrete;
using RTSFramework.RTSApproaches.Utilities;
using Unity.Attributes;

namespace RTSFramework.Controller
{
	public class RTSController<TPeDiscoverer, TPeRTSApproach, TPDiscoverer, TTc> : IRTSListener<TTc> where TPeDiscoverer : IProgramModelElement where TPeRTSApproach : IProgramModelElement where TTc : ITestCase where TPDiscoverer : IProgramModel
	{
	    private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>>> deltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory;
        private readonly ITestsDiscoverer<TTc> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TPeRTSApproach, TTc>> rtsApproachFactory;

        [Dependency]
        public Lazy<IDeltaAdapter<TPeDiscoverer, TPeRTSApproach>> DeltaAdapter { get; set; }

        public RTSController(
            Func<DiscoveryType, IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>>> deltaDiscovererFactory,
            Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory,
            ITestsDiscoverer<TTc> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TPeRTSApproach, TTc>> rtsApproachFactory, 
            DynamicMapManager dynamicMapUpdater)
        {
            this.deltaDiscovererFactory = deltaDiscovererFactory;
            this.testProcessorFactory = testProcessorFactory;
            this.testsDiscoverer = testsDiscoverer;
            this.rtsApproachFactory = rtsApproachFactory;
        }

        private static void GetTestAssemblies(string folder, List<string> testAssemblies)
        {
            //TODO More advanced filtering for test assemblies?
            foreach (var assembly in Directory.GetFiles(folder, "*Test.dll"))
            {
                var fileName = Path.GetFileName(assembly);
                if (testAssemblies.All(x => !x.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    testAssemblies.Add(assembly);
                }
            }

            foreach (var subFolder in Directory.GetDirectories(folder))
            {
                GetTestAssemblies(subFolder, testAssemblies);
            }
        }

	    private ITestProcessor<TTc> InitializeTestProcessor(RunConfiguration<TPDiscoverer> configuration)
	    {
	        return testProcessorFactory(configuration.ProcessingType);
        }

	    private void InitializeTestFramework(RunConfiguration<TPDiscoverer> configuration)
	    {
            var testAssemblies = new List<string>();

            foreach (var folder in configuration.TestAssemblyFolders)
            {
                GetTestAssemblies(folder, testAssemblies);
            }

            testsDiscoverer.Sources = testAssemblies;
	    }

	    private IRTSApproach<TPeRTSApproach, TTc> InitializeRTSApproach(RunConfiguration<TPDiscoverer> configuration)
	    {
	        return rtsApproachFactory(configuration.RTSApproachType);
	    }


        private StructuralDelta<TPeDiscoverer> PerformDeltaDiscovery(RunConfiguration<TPDiscoverer> configuration)
        {
            var deltaDiscoverer = deltaDiscovererFactory(configuration.DiscoveryType);

            StructuralDelta<TPeDiscoverer> delta = default(StructuralDelta<TPeDiscoverer>);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
                () => delta = deltaDiscoverer.Discover(configuration.OldProgramModel, configuration.NewProgramModel), "DeltaDiscovery");

	        return delta;
	    }

        public void ExecuteImpactedTests(RunConfiguration<TPDiscoverer> configuration)
        {
            var testProcessor = InitializeTestProcessor(configuration);
            var rtsApproach = InitializeRTSApproach(configuration);
            InitializeTestFramework(configuration);

            var delta = PerformDeltaDiscovery(configuration);

            IEnumerable<TTc> allTests = null;
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => allTests = testsDiscoverer.GetTestCases(),
                "TestsDiscovery");
            //TODO Filtering of tests
            //var defaultCategory = allTests.Where(x => x.Categories.Any(y => y == "Default"));

            StructuralDelta<TPeRTSApproach> convertedDelta = delta as StructuralDelta<TPeRTSApproach> ?? DeltaAdapter.Value.Convert(delta);

            rtsApproach.RegisterImpactedTestObserver(this);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => rtsApproach.ExecuteRTS(allTests, convertedDelta),
                "RTSApproach");
            rtsApproach.UnregisterImpactedTestObserver(this);
            Console.WriteLine($"{impactedTests.Count} Tests impacted");

	        ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
	            () => testProcessor.ProcessTests(impactedTests), "ProcessingOfImpactedTests");
	        

	        var processorWithCoverageCollection = testProcessor as IAutomatedTestsExecutorWithCoverageCollection<TTc>;
            var coverageResults = processorWithCoverageCollection?.GetCollectedCoverageData();
	        if (coverageResults != null)
            {
                var dynamicRtsApproach = rtsApproach as DynamicRTSApproach<TPeRTSApproach, TTc>;
                dynamicRtsApproach?.UpdateMap(coverageResults);
	        }

	        var automatedTestsProcessor = testProcessor as IAutomatedTestsExecutor<TTc>;
	        if (automatedTestsProcessor != null)
	        {
                var testResults = automatedTestsProcessor.GetResults();
                ReportFinalResultsToConsole(testResults);
            }
	    }

        private readonly List<TTc> impactedTests = new List<TTc>();

	    public void NotifyImpactedTest(TTc impactedTest)
	    {
            impactedTests.Add(impactedTest);
        }

        private void ReportFinalResultsToConsole(IEnumerable<ITestCaseResult<TTc>> results)
        {
            Console.WriteLine();
            Console.WriteLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<TTc>> ?? results.ToList();
            foreach (var result in testCaseResults)
            {
                Console.WriteLine($"{result.AssociatedTestCase.Id}: {result.Outcome}");
            }
            int numberOfTestsNotPassed = testCaseResults.Count(x => x.Outcome != TestCaseResultType.Passed);

            Console.WriteLine();
            Console.WriteLine(numberOfTestsNotPassed == 0 ? $"All {testCaseResults.Count} tests passed!" : $"{numberOfTestsNotPassed} of {testCaseResults.Count} did not pass!");

            Console.ReadKey();
        }
    }
}
