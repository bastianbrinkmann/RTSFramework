using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class MSTestTestsExecutor : IAutomatedTestsExecutor<MSTestTestcase>
    {
        private readonly IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter;

        public MSTestTestsExecutor(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter)
        {
            this.resultArtefactAdapter = resultArtefactAdapter;
        }

        protected IList<MSTestTestcase> CurrentlyExecutedTests;
        protected IEnumerable<ITestCaseResult<MSTestTestcase>> ExecutionResults = new List<ITestCaseResult<MSTestTestcase>>();
        public virtual async Task ProcessTests(IEnumerable<MSTestTestcase> tests, CancellationToken cancellationToken = default(CancellationToken))
        {
            CurrentlyExecutedTests = tests as IList<MSTestTestcase> ?? tests.ToList();
            CurrentlyExecutedTests = CurrentlyExecutedTests.Where(x => !x.Ignored).ToList();
            if (CurrentlyExecutedTests.Any())
            {
                var arguments = BuildVsTestsArguments();

                await ExecuteVsTestsByArguments(arguments, cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

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
                var resultParams = new MSTestExecutionResultParameters {File = trxFile};
                resultParams.ExecutedTestcases.AddRange(CurrentlyExecutedTests);
                var results = resultArtefactAdapter.Parse(resultParams);

	            bool cleanUpResultDirectory = false; //TODO: App Config Setting
	            if (cleanUpResultDirectory)
	            {
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
				}

                return results;
            }

            throw new ArgumentException("Test Execution Failed as no trx file was created!");
        }

        protected async Task ExecuteVsTestsByArguments(string arguments, CancellationToken cancellationToken)
        {
            var executorProccess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole),
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = false
                }
            };

            executorProccess.Start();
	        try
	        {
				await executorProccess.WaitForExitAsync(cancellationToken);
			}
			catch (OperationCanceledException) { }
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