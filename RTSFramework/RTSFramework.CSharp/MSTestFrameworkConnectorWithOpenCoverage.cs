using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnectorWithOpenCoverage : MSTestFrameworkConnector, IAutomatedTestFrameworkWithCoverageCollection<MSTestTestcase>
    {
        private const string OpenCoverExe = "OpenCover.Console.exe";
        private const string OpenCoverPath = "OpenCover";

        public MSTestFrameworkConnectorWithOpenCoverage(IEnumerable<string> sources) : base(sources)
        {
        }

        private ICoverageData coverageData;

        public override void ProcessTests(IEnumerable<MSTestTestcase> tests)
        {
            CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
            if (CurrentlyExecutedTests.Any())
            {
				var vsTestArguments = BuildVsTestsArguments();
				var openCoverArguments = BuildOpenCoverArguments(vsTestArguments);

                ExecuteOpenCoverByArguments(openCoverArguments);

                var executionResult = ParseVsTestsTrxAnswer();

                coverageData = OpenCoverXmlParser.Parse(Path.GetFullPath(@"results.xml"), CurrentlyExecutedTests);

                ExecutionResults = executionResult.TestcasesResults;
            }
        }

		

        private  string BuildOpenCoverArguments(string vstestargs)
        {
            string targetArg = "\"-target:" + Path.Combine(VstestPath, Vstestconsole) + "\"";
            string targetArgsArg = "\"-targetargs:" + vstestargs + "\"";
            string registerArg = "-register:user";
	        string logArg = "-log:Off";
	        string hideSkippedArg = "-hideskipped:All";
			string coverbytestArg = "-coverbytest:" + string.Join(";", Sources.Select(x => @"*Out\" + Path.GetFileName(x)));

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
                    RedirectStandardOutput = true
                }
            };

            discovererProcess.OutputDataReceived += DiscovererProcessOnOutputDataReceived;

            discovererProcess.Start();
            discovererProcess.BeginOutputReadLine();

            discovererProcess.WaitForExit();
        }

        public ICoverageData GetCollectedCoverageData()
        {
            return coverageData;
        }
    }
}