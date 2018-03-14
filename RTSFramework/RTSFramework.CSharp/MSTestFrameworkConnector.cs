using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnector : IAutomatedTestFramework<MSTestTestcase>
    {
        private const string VstestPath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow";
        private const string Vstestconsole = @"vstest.console.exe";
        private const string MSTestAdapterPath = @"C:\Git\RTSFramework\RTSFramework\packages\MSTest.TestAdapter.1.2.0\build\_common";

        private List<MSTestTestcase> testCases;
        private readonly IEnumerable<string> sources;

        private const string TestMethodAttributeName = "TestMethodAttribute";
        private const string TestCategoryAttributeName = "TestCategoryAttribute";

        protected const string TestResultsFolder = @"TestResults";
        public MSTestFrameworkConnector(IEnumerable<string> sources)
        {
            this.sources = sources;
        }

        public IEnumerable<MSTestTestcase> GetTestCases()
        {
            if (testCases == null)
            {
                testCases = new List<MSTestTestcase>();

                foreach (var modulePath in sources)
                {
                    ModuleDefinition module = GetModuleDefinition(modulePath);
                    foreach (TypeDefinition type in module.Types)
                    {
                        if (type.HasMethods)
                        {
                            foreach (MethodDefinition method in type.Methods)
                            {
                                if (method.CustomAttributes.Any(x => x.AttributeType.Name == TestMethodAttributeName))
                                {
                                    var declaringTypeFull = method.DeclaringType.FullName;

                                    var testCase = new MSTestTestcase($"{declaringTypeFull}.{method.Name}");

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
            }

            return testCases;
        }

        private ModuleDefinition GetModuleDefinition(string moduleFilePath)
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

        public IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        {
            return ExecuteTestsInternal(tests);
        }

        protected virtual IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTestsInternal(IEnumerable<MSTestTestcase> tests)
        {
            var testsFullyQualifiedNames = tests.Select(x => x.Id).ToList();
            if (testsFullyQualifiedNames.Any())
            {
                var arguments = BuildArguments(testsFullyQualifiedNames);

                ExecuteVsTestsByArguments(arguments);

                return ParseVsTestsTrxAnswer(tests).TestcasesResults;
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

                trxFile.Delete();
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
                    UseShellExecute = false
                }
            };

            discovererProcess.Start();
            discovererProcess.WaitForExit();
        }

        protected string BuildArguments(List<string> testsFullyQualifiedNames)
        {
            string testCaseFilterArg = "/TestCaseFilter:";
            testCaseFilterArg += "FullyQualifiedName=" + string.Join("|FullyQualifiedName=", testsFullyQualifiedNames);
            string testAdapterPathArg = "/TestAdapterPath:" + MSTestAdapterPath;
            string sourcesArg = string.Join(" ", sources);
            string loggerArg = "/logger:trx";
            string arguments = testAdapterPathArg + " " + testCaseFilterArg + " " + sourcesArg + " " + loggerArg;

            return arguments;
        }
    }
}