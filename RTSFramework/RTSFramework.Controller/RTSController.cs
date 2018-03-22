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
using Unity.Attributes;

namespace RTSFramework.Controller
{
	public class RTSController<TPeDiscoverer, TPeRTSApproach, TPDiscoverer, TTc> : IRTSListener<TTc> where TPeDiscoverer : IProgramModelElement where TPeRTSApproach : IProgramModelElement where TTc : ITestCase where TPDiscoverer : IProgramModel
	{
	    private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>>> deltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory;
        private readonly ITestFramework<TTc> testFramework;
        private readonly Func<RTSApproachType, IRTSApproach<TPeRTSApproach, TTc>> rtsApproachFactory;
	    private readonly IDynamicMapUpdater dynamicMapUpdater;

        [Dependency]
        public Lazy<IDeltaAdapter<TPeDiscoverer, TPeRTSApproach>> DeltaAdapter { get; set; }

        public RTSController(
            Func<DiscoveryType, IOfflineDeltaDiscoverer<TPDiscoverer, StructuralDelta<TPeDiscoverer>>> deltaDiscovererFactory,
            Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory,
            ITestFramework<TTc> testFramework,
            Func<RTSApproachType, IRTSApproach<TPeRTSApproach, TTc>> rtsApproachFactory, 
            IDynamicMapUpdater dynamicMapUpdater)
        {
            this.deltaDiscovererFactory = deltaDiscovererFactory;
            this.testProcessorFactory = testProcessorFactory;
            this.testFramework = testFramework;
            this.rtsApproachFactory = rtsApproachFactory;
            this.dynamicMapUpdater = dynamicMapUpdater;
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

            testFramework.Sources = testAssemblies;
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
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => allTests = testFramework.GetTestCases(),
                "GettingTestcases");
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
	        

	        var processorWithCoverageCollection = testProcessor as IAutomatedTestFrameworkWithCoverageCollection<TTc>;
	        var coverageResults = processorWithCoverageCollection?.GetCollectedCoverageData();
	        if (coverageResults != null)
	        {
                dynamicMapUpdater.UpdateDynamicMap(coverageResults, configuration.OldProgramModel.VersionId, configuration.NewProgramModel.VersionId, allTests.Select(x => x.Id).ToList());
	        }

	        var automatedTestsProcessor = testProcessor as IAutomatedTestFramework<TTc>;
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
            Console.WriteLine(numberOfTestsNotPassed == 0 ? "All tests passed!" : $"{numberOfTestsNotPassed} of {testCaseResults.Count()} did not pass!");

            Console.ReadKey();
        }
    }
}
