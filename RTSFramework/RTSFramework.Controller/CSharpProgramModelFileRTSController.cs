﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Models;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.Dynamic;

namespace RTSFramework.Controller
{
    public class CSharpProgramModelFileRTSController<TPe, TP, TTc> : IRTSListener<TTc> where TPe : IProgramModelElement
        where TTc : ITestCase
        where TP : CSharpProgramModel
    {
        private readonly Func<DiscoveryType, IOfflineDeltaDiscoverer<TP, StructuralDelta<TP, TPe>>> filedeltaDiscovererFactory;
        private readonly Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory;
        private readonly ITestsDiscoverer<TTc> testsDiscoverer;
        private readonly Func<RTSApproachType, IRTSApproach<TP, TPe, TTc>> rtsApproachFactory;
        private readonly IArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;

        public CSharpProgramModelFileRTSController(
            Func<DiscoveryType, IOfflineDeltaDiscoverer<TP, StructuralDelta<TP, TPe>>> filedeltaDiscovererFactory,
            Func<ProcessingType, ITestProcessor<TTc>> testProcessorFactory,
            ITestsDiscoverer<TTc> testsDiscoverer,
            Func<RTSApproachType, IRTSApproach<TP, TPe, TTc>> rtsApproachFactory,
            IArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter)
        {
            this.filedeltaDiscovererFactory = filedeltaDiscovererFactory;
            this.testProcessorFactory = testProcessorFactory;
            this.testsDiscoverer = testsDiscoverer;
            this.rtsApproachFactory = rtsApproachFactory;
            this.assembliesAdapter = assembliesAdapter;
        }

        private ITestProcessor<TTc> InitializeTestProcessor(RunConfiguration<TP> configuration)
        {
            return testProcessorFactory(configuration.ProcessingType);
        }

        private void InitializeTestFramework(RunConfiguration<TP> configuration)
        {
            //TODO Filtering of Test dlls?
            testsDiscoverer.Sources = assembliesAdapter.Parse(configuration.AbsoluteSolutionPath).Select(x => x.AbsolutePath)
                .Where(x => x.EndsWith("Test.dll"));
        }

        private IRTSApproach<TP, TPe, TTc> InitializeRTSApproach(RunConfiguration<TP> configuration)
        {
            return rtsApproachFactory(configuration.RTSApproachType);
        }


        private StructuralDelta<TP, TPe> PerformDeltaDiscovery(RunConfiguration<TP> configuration)
        {
            var deltaDiscoverer = filedeltaDiscovererFactory(configuration.DiscoveryType);

            StructuralDelta<TP, TPe> delta = default(StructuralDelta<TP, TPe>);
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
                var dynamicRtsApproach = rtsApproach as DynamicRTSApproach<TP, TPe, TTc>;
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
            Console.WriteLine($"Impacted Test: {impactedTest.Id}");
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