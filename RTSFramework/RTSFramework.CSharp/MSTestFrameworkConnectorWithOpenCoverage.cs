using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.RTSApproaches.Utilities;

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

        public override IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        {
            var msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();
            if (msTestTestcases.Any())
            {
				var vsTestArguments = BuildVsTestsArguments(msTestTestcases);
				var openCoverArguments = BuildOpenCoverArguments(vsTestArguments);

                ExecuteOpenCoverByArguments(openCoverArguments);

                var executionResult = ParseVsTestsTrxAnswer(msTestTestcases);

                coverageData = OpenCoverXmlParser.Parse(Path.GetFullPath(@"results.xml"), Sources, msTestTestcases);

                return executionResult.TestcasesResults;
            }

            return new List<ITestCaseResult<MSTestTestcase>>();
        }

		

        private  string BuildOpenCoverArguments(string vstestargs)
        {
            string targetArg = "\"-target:" + Path.Combine(VstestPath, Vstestconsole) + "\"";
            string targetArgsArg = "\"-targetargs:" + vstestargs + "\"";
            string registerArg = "-register:user";
	        string logArg = "-log:Off";
	        string hideSkippedArg = "-hideskipped:All";
			string coverbytestArg = "-coverbytest"; //":" + string.Join(";", Sources);

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