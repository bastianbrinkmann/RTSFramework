using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnector : IAutomatedTestFramework<MSTestTestcase>, ITestCaseDiscoverySink, IMessageLogger
    {
        private const string VstestPath = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
        private const string Vstestconsole = @"vstest.console.exe";
        private const string MSTestAdapterPath = @"C:\Git\RTSFramework\RTSFramework\packages\MSTest.TestAdapter.1.2.0\build\_common";

        private List<MSTestTestcase> testCases;
        private IEnumerable<string> sources;

        public MSTestFrameworkConnector(IEnumerable<string> sources)
        {
            this.sources = sources;
        }

        public IEnumerable<MSTestTestcase> GetTestCases()
        {
            //testCases = new List<MSTestTestcase>();

            //string testAdapterPathArg = "/TestAdapterPath:" + MSTestAdapterPath;
            //foreach (var source in sources)
            //{
            //    string listTestsArg = "/ListTests:" + source;

            //    var discovererProcess = new Process
            //    {
            //        StartInfo = new ProcessStartInfo
            //        {
            //            FileName = Path.Combine(VstestPath, Vstestconsole),
            //            Arguments = testAdapterPathArg +  " " + listTestsArg,
            //            UseShellExecute = false,
            //            RedirectStandardOutput = true,
            //            CreateNoWindow = true
            //        }
            //    };

            //    discovererProcess.Start();

            //    bool testsListed = false;
            //    while (!discovererProcess.StandardOutput.EndOfStream)
            //    {
            //        string line = discovererProcess.StandardOutput.ReadLine();
            //        if (line.Contains("The following Tests are available:"))
            //        {
            //            testsListed = true;
            //            continue;
            //        }

            //        if (testsListed && !string.IsNullOrEmpty(line.Trim()))
            //        {
            //            testCases.Add(new MSTestTestcase(line.Trim(), source));
            //        }
            //    }

            //}

            if (testCases == null)
            {
                testCases = new List<MSTestTestcase>();

                var discoverer = new MSTestDiscoverer();
                discoverer.DiscoverTests(sources, null, this, this);
            }

            return testCases;
        }

        public IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        { 
            var testsFullyQualifiedNames = tests.Select(x => x.Id).ToList();
            if (testsFullyQualifiedNames.Any())
            {
                string testCaseFilterArg = "/TestCaseFilter:";
                testCaseFilterArg += "FullyQualifiedName=" + string.Join("|FullyQualifiedName=", testsFullyQualifiedNames);
                string testAdapterPathArg = "/TestAdapterPath:" + MSTestAdapterPath;
                string sourcesArg = string.Join(" ", this.sources);
                string loggerArg = "/logger:trx";

                var discovererProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine(VstestPath, Vstestconsole),
                        Arguments = testAdapterPathArg + " " + testCaseFilterArg + " " + sourcesArg + " " + loggerArg,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };

                discovererProcess.Start();

                discovererProcess.WaitForExit();
            }

            var testResultsFolder = @"TestResults";
            var directory = new DirectoryInfo(testResultsFolder);
            if (directory.Exists)
            {
                var myFile = (from f in directory.GetFiles()
                              where f.Name.EndsWith(".trx")
                              orderby f.LastWriteTime descending
                              select f).FirstOrDefault();
                if (myFile != null)
                {
                    var results = TrxFileParser.Parse(myFile.FullName, testCases);

                    myFile.Delete();

                    return results;
                }
            }

            throw new ArgumentException("Test Execution Failed!");
        }

        public void SendTestCase(TestCase discoveredTest)
        {
            testCases.Add(new MSTestTestcase(discoveredTest.FullyQualifiedName));
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            //TODO Handle?
        }
    }
}