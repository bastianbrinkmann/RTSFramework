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
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class ConsoleMSTestTestExecutor<TDelta, TModel> : ITestExecutor<MSTestTestcase, TDelta, TModel> where TDelta : IDelta<TModel> where TModel : IProgramModel
    {
        private readonly IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter;
	    private readonly ISettingsProvider settingsProvider;

	    public ConsoleMSTestTestExecutor(IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult> resultArtefactAdapter,
			ISettingsProvider settingsProvider)
	    {
		    this.resultArtefactAdapter = resultArtefactAdapter;
		    this.settingsProvider = settingsProvider;
	    }

        protected IList<MSTestTestcase> CurrentlyExecutedTests;

        public virtual async Task<ITestsExecutionResult<MSTestTestcase>> ProcessTests(IList<MSTestTestcase> impactedTests, ISet<MSTestTestcase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
	        var executionResult = new MSTestExectionResult();

	        CurrentlyExecutedTests = impactedTests;
            CurrentlyExecutedTests = CurrentlyExecutedTests.Where(x => !x.Ignored).ToList();
            if (CurrentlyExecutedTests.Any())
            {
                var arguments = BuildVsTestsArguments();

                await ExecuteVsTestsByArguments(arguments, cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();

				executionResult = ParseVsTestsTrxAnswer();
            }

	        return executionResult;
        }

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

	            if (settingsProvider.CleanupTestResultsDirectory)
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
                    FileName = Path.GetFullPath(Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole)),
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

            string loggerArg = "/logger:trx";
            string arguments = loggerArg + " " + orderedTestsPath;

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
                continueAfterFailure = true
            };

            var serializer = new XmlSerializer(typeof(OrderedTestType));
            using (var stream = info.OpenWrite())
            {
                serializer.Serialize(stream, testType);
            }

            return fullPath;
        }

		public event EventHandler<TestCaseResultEventArgs<MSTestTestcase>> TestResultAvailable;
    }
}