using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Concrete.User;
using RTSFramework.Console.RunConfigurations;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core;
using RTSFramework.Core.Artefacts;
using RTSFramework.RTSApproaches.Concrete;
using Unity;

namespace RTSFramework.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new RunConfiguration
            {
                DiscoveryType = DiscoveryType.UserBasedDiscovery,
                ProgramModelType = ProgramModelType.TFS2010ProgramModel,
                GitRepositoryPath = @"C:\Git\TIATestProject",
                IntendedChanges = new[] {@"C:\Git\TIATestProject\MainProject\Calculator.cs"},
                TestAssemblyFolders = new[] {@"C:\Git\TIATestProject\MainProject.Test\bin\Debug\"},
                RTSApproachType = RTSApproachType.DynamicRTS,
                PersistDynamicMap = true,
            };

            var container = new UnityContainer();
            UnityInitializer.Init(container, configuration);

            if (configuration.ProgramModelType == ProgramModelType.GitProgramModel)
            {
                GitExampleRun(container, configuration);
            }
            else if (configuration.ProgramModelType == ProgramModelType.TFS2010ProgramModel)
            {
                TFS2010ExampleRun(container, configuration);
            }

            Process.Start(Directory.GetCurrentDirectory());
        }

        private static void TFS2010ExampleRun(IUnityContainer container, RunConfiguration configuration)
        {
            var oldProgramModel = new TFS2010ProgramModel
            {
                VersionId = "Test"
            };
            var newProgramModel = new TFS2010ProgramModel
            {
                VersionId = "Test2"
            };

            var controller = container.Resolve<DynamicRTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();

            var results = controller.ExecuteImpactedTests(oldProgramModel, newProgramModel);
            ReportFinalResults(results);
        }

        private static void GitExampleRun(IUnityContainer container, RunConfiguration configuration)
        {
            var oldProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.LatestCommit);
            var newProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.CurrentChanges);

            var controller = container.Resolve<DynamicRTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();

            var results = controller.ExecuteImpactedTests(oldProgramModel, newProgramModel);
            ReportFinalResults(results);
        }

        private static void ReportFinalResults(IEnumerable<ITestCaseResult<MSTestTestcase>> results)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Final more detailed Test Results:");

            var testCaseResults = results as IList<ITestCaseResult<MSTestTestcase>> ?? results.ToList();
            foreach (var result in testCaseResults)
            {
                System.Console.WriteLine($"{result.AssociatedTestCase.Id}: {result.Outcome}");
            }
            int numberOfFailedTests = testCaseResults.Count(x => x.Outcome == TestCaseResultType.Failed);

            System.Console.WriteLine();
            System.Console.WriteLine(numberOfFailedTests == 0 ? "All tests passed!" : $"{numberOfFailedTests} of {testCaseResults.Count()} failed!");

            System.Console.ReadKey();
        }
    }
}
