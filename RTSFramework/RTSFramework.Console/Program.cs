using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.User;
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
            string repositoryPath = @"C:\Git\TIATestProject";

            var latestCommitId = CommitIdentifierHelper.GetVersionIdentifier(repositoryPath,
                VersionReferenceType.LatestCommit);
            var uncomittedId = CommitIdentifierHelper.GetVersionIdentifier(repositoryPath,
                VersionReferenceType.CurrentChanges);

            GitProgramModel oldProgramModel = new GitProgramModel
            {
                VersionReferenceType = VersionReferenceType.LatestCommit,
                VersionId = latestCommitId
            };
            GitProgramModel newProgramModel = new GitProgramModel
            {
                VersionReferenceType = VersionReferenceType.CurrentChanges,
                VersionId = uncomittedId
            };

            var container = new UnityContainer();
            UnityInitializer.Init(container, repositoryPath);

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
