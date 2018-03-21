using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnector : IAutomatedTestFramework<MSTestTestcase>
    {
        protected readonly string VstestPath = Path.Combine(
            Environment.GetEnvironmentVariable("VS140COMNTOOLS") ?? @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools",
            @"..\IDE\CommonExtensions\Microsoft\TestWindow");
        protected const string Vstestconsole = @"vstest.console.exe";
        private const string MSTestAdapterPath = @"MSTestAdapter";

        protected readonly IEnumerable<string> Sources;

        private const string TestMethodAttributeName = "TestMethodAttribute";
        private const string TestCategoryAttributeName = "TestCategoryAttribute";

        protected virtual string TestResultsFolder => @"TestResults";
        public MSTestFrameworkConnector(IEnumerable<string> sources)
        {
            this.Sources = sources;
        }

        public IEnumerable<MSTestTestcase> GetTestCases()
        {
            var testCases = new List<MSTestTestcase>();

            foreach (var modulePath in Sources)
            {
                ModuleDefinition module = GetMonoModuleDefinition(modulePath);
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.HasMethods)
                    {
                        foreach (MethodDefinition method in type.Methods)
                        {
                            if (method.CustomAttributes.Any(x => x.AttributeType.Name == TestMethodAttributeName))
                            {
                                var declaringTypeFull = method.DeclaringType.FullName;

                                var testCase = new MSTestTestcase($"{declaringTypeFull}.{method.Name}", modulePath, method.Name);

                                var categoryAttributes =
                                    method.CustomAttributes.Where(x => x.AttributeType.Name == TestCategoryAttributeName);
                                foreach (var categoryAttr in categoryAttributes)
                                {
                                    testCase.Categories.Add((string)categoryAttr.ConstructorArguments[0].Value);
                                }

                                testCases.Add(testCase);
                            }
                        }
                    }
                }
            }

            return testCases;
        }

        private ModuleDefinition GetMonoModuleDefinition(string moduleFilePath)
        {
            FileInfo fileInfo = new FileInfo(moduleFilePath);
            if (!fileInfo.Exists)
            {
                throw new ArgumentException("Invalid path to a module: " + moduleFilePath);
            }
            ModuleDefinition module;

            ReaderParameters parameters = new ReaderParameters { ReadSymbols = true };
            try
            {
                module = ModuleDefinition.ReadModule(moduleFilePath, parameters);
            }
            catch (Exception)
            {
                module = ModuleDefinition.ReadModule(moduleFilePath);
            }

            return module;
        }

        private IList<MSTestTestcase> msTestTestcases;
        public virtual IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        {
            msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();
            if (msTestTestcases.Any())
            {
                var arguments = BuildVsTestsArguments(msTestTestcases);

                ExecuteVsTestsByArguments(arguments);

                return ParseVsTestsTrxAnswer(msTestTestcases).TestcasesResults;
            }

            return new List<ITestCaseResult<MSTestTestcase>>();
        }

        protected FileInfo GetTrxFile()
        {
            var directory = new DirectoryInfo(TestResultsFolder);
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

        protected MSTestExectionResult ParseVsTestsTrxAnswer(IEnumerable<MSTestTestcase> tests)
        {
            var trxFile = GetTrxFile();
            if (trxFile != null)
            {
                var results = TrxFileParser.Parse(trxFile.FullName, tests);

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
                    FileName = Path.Combine(VstestPath, Vstestconsole),
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

            //TODO evaluate whether Vstest output can be analyzed more detailed -> e.g. empty row means new class
            if (dataReceivedEventArgs.Data != null)
            {
                Console.WriteLine(dataReceivedEventArgs.Data);
            }
        }

		protected string BuildVsTestsArguments(IList<MSTestTestcase> msTestTestcases)
		{
			var orderedTestsPath = CreateOrderTestsFile(msTestTestcases);

			string testAdapterPathArg = "/TestAdapterPath:" + Path.GetFullPath(MSTestAdapterPath);
			string loggerArg = "/logger:trx";
			string arguments = testAdapterPathArg + " " + loggerArg + " " + orderedTestsPath;

			return arguments;
		}

		// convert the test (<Name space name>.<class name>.<test method name>) to a GUID
		// https://blogs.msdn.microsoft.com/aseemb/2013/10/05/how-to-create-an-ordered-test-programmatically/
		private static Guid ComputeMsTestCaseGuid(string data)
		{
			SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
			byte[] hash = provider.ComputeHash(System.Text.Encoding.Unicode.GetBytes(data));
			byte[] toGuid = new byte[16];
			Array.Copy(hash, toGuid, 16);
			return new Guid(toGuid);
		}

		private static string CreateOrderTestsFile(IList<MSTestTestcase> msTests)
		{
			string fileName = "testrun.orderedtest";
			string fullPath = Path.GetFullPath(fileName);
			FileInfo info = new FileInfo(fullPath);
			if (info.Exists)
			{
				info.Delete();
			}

			var testLinks = new List<LinkType>();

			foreach (MSTestTestcase testcase in msTests)
			{
				testLinks.Add(new LinkType
				{
					id = ComputeMsTestCaseGuid(testcase.Id).ToString(),
					name = testcase.Name,
					storage = testcase.AssemblyPath,
					//TODO Get Type and then to string
					type = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement, Microsoft.VisualStudio.QualityTools.Tips.UnitTest.ObjectModel, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
				});
			}

			var testType = new OrderedTestType
			{
				id = Guid.NewGuid().ToString(),
				storage = fullPath,
				name = "Testrun",
				TestLinks = testLinks.ToArray()
			};

			var serializer = new XmlSerializer(typeof(OrderedTestType));
			using (var stream = info.OpenWrite())
			{
				serializer.Serialize(stream, testType);
			}

			return fullPath;
		}

		//protected string BuildVsTestsArguments(List<string> testsFullyQualifiedNames)
		//{
		//    string testCaseFilterArg = "/TestCaseFilter:";
		//    testCaseFilterArg += "FullyQualifiedName=" +
		//                         string.Join("|FullyQualifiedName=", testsFullyQualifiedNames);
		//    string testAdapterPathArg = "/TestAdapterPath:" + Path.GetFullPath(MSTestAdapterPath);
		//    string sourcesArg = string.Join(" ", Sources);
		//    string loggerArg = "/logger:trx";
		//    string arguments = testAdapterPathArg + " " + testCaseFilterArg + " " + sourcesArg + " " + loggerArg;

		//    return arguments;
		//}
	}
}