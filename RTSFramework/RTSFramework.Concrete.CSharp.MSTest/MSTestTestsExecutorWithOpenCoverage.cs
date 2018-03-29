using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.CSharp.Core.Artefacts;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class MSTestTestsExecutorWithOpenCoverage : MSTestTestsExecutor, IAutomatedTestsExecutorWithCoverageCollection<MSTestTestcase>
    {
        private readonly IArtefactAdapter<MSTestExecutionResultParameters, ICoverageData> openCoverArtefactAdapter;


        private const string OpenCoverExe = "OpenCover.Console.exe";
        private const string OpenCoverPath = "OpenCover";

        private ICoverageData coverageData;

        public MSTestTestsExecutorWithOpenCoverage(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter,
                                                    IArtefactAdapter<MSTestExecutionResultParameters, ICoverageData> openCoverArtefactAdapter) : base(resultArtefactAdapter)
        {
            this.openCoverArtefactAdapter = openCoverArtefactAdapter;
        }

        public override void ProcessTests(IEnumerable<MSTestTestcase> tests)
        {
            CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
            if (CurrentlyExecutedTests.Any())
            {
				var vsTestArguments = BuildVsTestsArguments();

                var sources = CurrentlyExecutedTests.Select(x => x.AssemblyPath).Distinct();
				var openCoverArguments = BuildOpenCoverArguments(vsTestArguments, sources);

                ExecuteOpenCoverByArguments(openCoverArguments);

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

        private void ExecuteOpenCoverByArguments(string arguments)
        {
            var discovererProcess = new Process
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

            discovererProcess.Start();
            discovererProcess.WaitForExit();
        }

        public ICoverageData GetCollectedCoverageData()
        {
            return coverageData;
        }
    }
}