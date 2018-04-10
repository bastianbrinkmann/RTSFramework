﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Core;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class MSTestTestsExecutorWithOpenCoverage : MSTestTestsExecutor, IAutomatedTestsExecutorWithCoverageCollection<MSTestTestcase>
    {
        private readonly IArtefactAdapter<MSTestExecutionResultParameters, CoverageData> openCoverArtefactAdapter;


        private const string OpenCoverExe = "OpenCover.Console.exe";
        private const string OpenCoverPath = "OpenCover";

        private CoverageData coverageData;

        public MSTestTestsExecutorWithOpenCoverage(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter,
                                                    IArtefactAdapter<MSTestExecutionResultParameters, CoverageData> openCoverArtefactAdapter) : base(resultArtefactAdapter)
        {
            this.openCoverArtefactAdapter = openCoverArtefactAdapter;
        }

        public override async Task ProcessTests(IEnumerable<MSTestTestcase> tests, CancellationToken cancellationToken = default(CancellationToken))
        {
            CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
            if (CurrentlyExecutedTests.Any())
            {
				var vsTestArguments = BuildVsTestsArguments();

                var sources = CurrentlyExecutedTests.Select(x => x.AssemblyPath).Distinct();
				var openCoverArguments = BuildOpenCoverArguments(vsTestArguments, sources);

                await ExecuteOpenCoverByArguments(openCoverArguments, cancellationToken);
	            if (cancellationToken.IsCancellationRequested)
	            {
		            return;
	            }

                var executionResult = ParseVsTestsTrxAnswer();

                var executionResultParams = new MSTestExecutionResultParameters {File = new FileInfo(Path.GetFullPath(@"results.xml"))};
                executionResultParams.ExecutedTestcases.AddRange(CurrentlyExecutedTests);
                coverageData = openCoverArtefactAdapter.Parse(executionResultParams);

                ExecutionResults = executionResult.TestcasesResults;
            }
        }

        private  string BuildOpenCoverArguments(string vstestargs, IEnumerable<string> sources)
        {
            string targetArg = "\"-target:" + Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole) + "\"";
            string targetArgsArg = "\"-targetargs:" + vstestargs + "\"";
            string registerArg = "-register:user";
	        string logArg = "-log:Off";
	        string hideSkippedArg = "-hideskipped:All";
			string coverbytestArg = "-coverbytest:" + string.Join(";", sources.Select(x => @"*Out\" + Path.GetFileName(x)));

            string arguments = targetArg + " " + targetArgsArg + " " + registerArg + " " + logArg + " " + hideSkippedArg + " " + coverbytestArg;

            return arguments;
        }

        private async Task ExecuteOpenCoverByArguments(string arguments, CancellationToken cancellationToken)
        {
            var executorProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(Path.Combine(OpenCoverPath, OpenCoverExe)),
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = false
                }
            };

            executorProcess.Start();
			try
			{
				await executorProcess.WaitForExitAsync(cancellationToken);
			}
			catch (OperationCanceledException) { }
		}

        public CoverageData GetCollectedCoverageData()
        {
            return coverageData;
        }
    }
}