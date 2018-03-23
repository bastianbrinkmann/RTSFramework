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

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class MSTestTestsExecutor : IAutomatedTestsExecutor<MSTestTestcase>
    {
        protected IList<MSTestTestcase> CurrentlyExecutedTests;
        protected IEnumerable<ITestCaseResult<MSTestTestcase>> ExecutionResults = new List<ITestCaseResult<MSTestTestcase>>();
        public virtual void ProcessTests(IEnumerable<MSTestTestcase> tests)
        {
            CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
            CurrentlyExecutedTests = CurrentlyExecutedTests.Where(x => !x.Ignored).ToList();
            if (CurrentlyExecutedTests.Any())
            {
                var arguments = BuildVsTestsArguments();

                ExecuteVsTestsByArguments(arguments);

                ExecutionResults = ParseVsTestsTrxAnswer().TestcasesResults;
            }
        }

        //TODO Read filepath from console instead!
        protected FileInfo GetTrxFile()
        {
            var directory = new DirectoryInfo(MSTestConstants.TestResultsFolder);
            if (directory.Exists)
            {
                var myFile = (from f in directory.GetFiles()
                              where f.Name.EndsWith(".trx")
                              orderby f.LastWriteTime descending
                              select f).FirstOrDefault();
                return myFile;
            }

            return null;
        }

        protected MSTestExectionResult ParseVsTestsTrxAnswer()
        {
            var trxFile = GetTrxFile();
            if (trxFile != null)
            {
                var results = TrxFileParser.Parse(trxFile.FullName, CurrentlyExecutedTests);

                var resultsDirectory = trxFile.Directory;
                if (resultsDirectory != null)
                {
                    foreach (FileInfo file in resultsDirectory.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception)
                        {
                            //Intentinally empty - vstestconsole sometimes locks files too long - cleaned up in next run then
                        }
                    }
                    foreach (DirectoryInfo dir in resultsDirectory.GetDirectories())
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (Exception)
                        {
                            //Intentinally empty - vstestconsole sometimes locks directories too long - cleaned up in next run then
                        }

                    }
                }

                return results;
            }

            throw new ArgumentException("Test Execution Failed as no trx file was created!");
        }

        protected void ExecuteVsTestsByArguments(string arguments)
        {
            var discovererProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole),
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

        protected void DiscovererProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            //TODO Report results on the fly instead of in the end

            var line = dataReceivedEventArgs.Data;
            if (line != null)
            {
                Console.WriteLine(line);
                if (line.StartsWith("Passed"))
                {
                    int number = OrderedTestsHelper.GetTestNumber(line);
                    var currentTest = CurrentlyExecutedTests[number - 1];
                }
                else if (line.StartsWith("Failed"))
                {
                    int number = OrderedTestsHelper.GetTestNumber(line);
                    var currentTest = CurrentlyExecutedTests[number - 1];
                }
            }
        }

        protected string BuildVsTestsArguments()
        {
            var orderedTestsPath = CreateOrderTestsFile();

            string testAdapterPathArg = "/TestAdapterPath:" + Path.GetFullPath(MSTestConstants.MSTestAdapterPath);
            string loggerArg = "/logger:trx";
            string arguments = testAdapterPathArg + " " + loggerArg + " " + orderedTestsPath;

            return arguments;
        }

        // convert the test (<Name space name>.<class name>.<test method name>) to a GUID
        // https://blogs.msdn.microsoft.com/aseemb/2013/10/05/how-to-create-an-ordered-test-programmatically/
        private Guid ComputeMsTestCaseGuid(string data)
        {
            SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
            byte[] hash = provider.ComputeHash(System.Text.Encoding.Unicode.GetBytes(data));
            byte[] toGuid = new byte[16];
            Array.Copy(hash, toGuid, 16);
            return new Guid(toGuid);
        }

        private string CreateOrderTestsFile()
        {
            string fileName = "testrun.orderedtest";
            string fullPath = Path.GetFullPath(fileName);
            FileInfo info = new FileInfo(fullPath);
            if (info.Exists)
            {
                info.Delete();
            }

            var testLinks = new List<LinkType>();
            int i = 1;
            foreach (MSTestTestcase testcase in CurrentlyExecutedTests)
            {
                testLinks.Add(new LinkType
                {
                    id = ComputeMsTestCaseGuid(testcase.Id).ToString(),
                    name = testcase.Name,
                    storage = testcase.AssemblyPath,
                    //Type needs to be referenced via String as the UnitTestElement class is internal
                    type = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel.UnitTestElement, Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel"
                });
                testcase.OrderedListPosition = i;
                i++;
            }

            var testType = new OrderedTestType
            {
                id = Guid.NewGuid().ToString(),
                storage = fullPath,
                name = "Testrun",
                TestLinks = testLinks.ToArray(),
                //TODO: Could be a configurable feature of AutomatedTestFramework
                continueAfterFailure = true
            };

            var serializer = new XmlSerializer(typeof(OrderedTestType));
            using (var stream = info.OpenWrite())
            {
                serializer.Serialize(stream, testType);
            }

            return fullPath;
        }

        public IEnumerable<ITestCaseResult<MSTestTestcase>> GetResults()
        {
            return ExecutionResults;
        }
    }
}