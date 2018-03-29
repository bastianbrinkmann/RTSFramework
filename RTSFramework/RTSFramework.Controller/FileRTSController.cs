using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Concrete;

namespace RTSFramework.Controller
{
    public class FileRTSController<TPe, TP, TTc> : IRTSListener<TTc> where TPe : IProgramModelElement
        where TTc : ITestCase
        where TP : IProgramModel
    {
        private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TP, StructuralDelta<TPe>>> filedeltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory;
        private readonly ITestsDiscoverer<TTc> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TPe, TTc>> rtsApproachFactory;

        public FileRTSController(
            Func<DiscoveryType, IOfflineDeltaDiscoverer<TP, StructuralDelta<TPe>>> filedeltaDiscovererFactory,
            Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory,
            ITestsDiscoverer<TTc> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TPe, TTc>> rtsApproachFactory)
        {
            this.filedeltaDiscovererFactory = filedeltaDiscovererFactory;
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

        private ITestProcessor<TTc> InitializeTestProcessor(RunConfiguration<TP> configuration)
        {
            return testProcessorFactory(configuration.ProcessingType);
        }

        private void InitializeTestFramework(RunConfiguration<TP> configuration)
        {
            var testAssemblies = new List<string>();

            foreach (var folder in configuration.TestAssemblyFolders)
            {
                GetTestAssemblies(folder, testAssemblies);
            }

            testsDiscoverer.Sources = testAssemblies;
        }

        private IRTSApproach<TPe, TTc> InitializeRTSApproach(RunConfiguration<TP> configuration)
        {
            return rtsApproachFactory(configuration.RTSApproachType);
        }


        private StructuralDelta<TPe> PerformDeltaDiscovery(RunConfiguration<TP> configuration)
        {
            var deltaDiscoverer = filedeltaDiscovererFactory(configuration.DiscoveryType);

            StructuralDelta<TPe> delta = default(StructuralDelta<TPe>);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
                () => delta = deltaDiscoverer.Discover(configuration.OldProgramModel, configuration.NewProgramModel), "DeltaDiscovery");

            return delta;
        }

        public void ExecuteImpactedTests(RunConfiguration<TP> configuration)
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

            rtsApproach.RegisterImpactedTestObserver(this);
            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(() => rtsApproach.ExecuteRTS(allTests, delta),
                "RTSApproach");
            rtsApproach.UnregisterImpactedTestObserver(this);
            Console.WriteLine($"{impactedTests.Count} Tests impacted");

            ConsoleStopWatchTracker.ReportNeededTimeOnConsole(
                () => testProcessor.ProcessTests(impactedTests), "ProcessingOfImpactedTests");


            var processorWithCoverageCollection = testProcessor as IAutomatedTestsExecutorWithCoverageCollection<TTc>;
            var coverageResults = processorWithCoverageCollection?.GetCollectedCoverageData();
            if (coverageResults != null)
            {
                var dynamicRtsApproach = rtsApproach as DynamicRTSApproach<TPe, TTc>;
                dynamicRtsApproach?.UpdateCorrespondenceModel(coverageResults);
            }

            var automatedTestsProcessor = testProcessor as IAutomatedTestsExecutor<TTc>;
            if (automatedTestsProcessor != null)
            {
                var testResults = automatedTestsProcessor.GetResults();
                ReportFinalResults(testResults);
            }
        }

        private readonly List<TTc> impactedTests = new List<TTc>();

        public void NotifyImpactedTest(TTc impactedTest)
        {
            impactedTests.Add(impactedTest);
        }

        private void ReportFinalResults(IEnumerable<ITestCaseResult<TTc>> results)
        {
            Console.WriteLine();
            Console.WriteLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<TTc>> ?? results.ToList();

            if (File.Exists("Error.log"))
            {
                File.Delete("Error.log");
            }

            using (var errorLog = File.Open("Error.log", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter logWriter = new StreamWriter(errorLog))
                {
                    logWriter.WriteLine("Failed Tests:");

                    foreach (var result in testCaseResults)
                    {
                        ReportTestResult(result, logWriter);
                    }
                }
            }

            int numberOfTestsNotPassed = testCaseResults.Count(x => x.Outcome != TestCaseResultType.Passed);


            Console.WriteLine();
            Console.WriteLine(numberOfTestsNotPassed == 0
                ? $"All {testCaseResults.Count} tests passed!"
                : $"{numberOfTestsNotPassed} of {testCaseResults.Count} did not pass!");

            Console.ReadKey();
        }

        private void ReportTestResult(ITestCaseResult<TTc> result, StreamWriter logWriter)
        {
            Console.WriteLine($"{result.TestCaseId}: {result.Outcome}");
            if (result.Outcome != TestCaseResultType.Passed)
            {
                logWriter.WriteLine($"{result.TestCaseId}: {result.Outcome} Message: {result.ErrorMessage} StackTrace: {result.StackTrace}");
            }

            int i = 0;
            foreach (var childResult in result.ChildrenResults)
            {
                Console.Write($"Data Row {i} - ");
                ReportTestResult(childResult, logWriter);
                i++;
            }
        }
    }
}