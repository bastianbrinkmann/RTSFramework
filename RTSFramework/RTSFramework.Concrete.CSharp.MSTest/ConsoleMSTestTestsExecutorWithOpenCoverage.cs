﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class ConsoleMSTestTestsExecutorWithOpenCoverage<TDelta, TModel> : ConsoleMSTestTestsExecutor<TDelta, TModel> where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		private readonly IArtefactAdapter<MSTestExecutionResultParameters, CoverageData> openCoverArtefactAdapter;

		private const string OpenCoverExe = "OpenCover.Console.exe";
		private const string OpenCoverPath = "OpenCover";

		public ConsoleMSTestTestsExecutorWithOpenCoverage(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter,
													IArtefactAdapter<MSTestExecutionResultParameters, CoverageData> openCoverArtefactAdapter,
													ISettingsProvider settingsProvider) : base(resultArtefactAdapter, settingsProvider)
		{
			this.openCoverArtefactAdapter = openCoverArtefactAdapter;
		}

		public override async Task<ITestExecutionResult<MSTestTestcase>> ProcessTests(IList<MSTestTestcase> impactedTests, IList<MSTestTestcase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
			var result = new MSTestExectionWithCodeCoverageResult();

			CurrentlyExecutedTests = impactedTests;
			if (CurrentlyExecutedTests.Any())
			{
				var vsTestArguments = BuildVsTestsArguments();

				var sources = CurrentlyExecutedTests.Select(x => x.AssemblyPath).Distinct();
				var openCoverArguments = BuildOpenCoverArguments(vsTestArguments, sources);

				await ExecuteOpenCoverByArguments(openCoverArguments, cancellationToken);

				var parsedResult = ParseVsTestsTrxAnswer();

				result.TestcasesResults.AddRange(parsedResult.TestcasesResults);

				var executionResultParams = new MSTestExecutionResultParameters { File = new FileInfo(Path.GetFullPath(@"results.xml")) };
				executionResultParams.ExecutedTestcases.AddRange(CurrentlyExecutedTests);

				var coverageData = openCoverArtefactAdapter.Parse(executionResultParams);
				result.CoverageData = coverageData;
			}

			return result;
		}

		private string BuildOpenCoverArguments(string vstestargs, IEnumerable<string> sources)
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
			await executorProcess.WaitForExitAsync(cancellationToken);
		}
	}
}