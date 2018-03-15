using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var testsFullyQualifiedNames = msTestTestcases.Select(x => x.Id).ToList();
            if (testsFullyQualifiedNames.Any())
            {
                var vsTestArguments = BuildVsTestsArguments(testsFullyQualifiedNames);
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
            string coverbytestArg = "-coverbytest:" + string.Join(";", Sources);

            string arguments = targetArg + " " + targetArgsArg + " " + registerArg + " " + coverbytestArg;

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
                    UseShellExecute = false
                }
            };

            Debug.WriteLine(discovererProcess.StartInfo.FileName + " " + discovererProcess.StartInfo.Arguments);

            discovererProcess.Start();
            discovererProcess.WaitForExit();
        }

        public ICoverageData GetCollectedCoverageData()
        {
            return coverageData;
        }
    }
}