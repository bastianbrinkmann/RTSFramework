using System.Collections.Generic;
using System.Linq;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.RTS;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;
using Unity;

namespace RTSFramework.Console
{
	class Program
	{
		static void Main(string[] args)
		{
		    //string solutionFile = @"C:\Git\TIATestProject\TIATestProject.sln";
		    string repositoryPath = @"C:\Git\TIATestProject";
            List<string> testAssemblies = new List<string>
            {
                @"C:\Git\TIATestProject\MainProject.Test\bin\Debug\MainProject.Test.dll"
            };

            var container = new UnityContainer();

		    container.RegisterInstance(typeof(IOfflineDeltaDiscoverer<GitProgramVersion, CSharpDocument, IDelta<CSharpDocument, GitProgramVersion>>), new LocalGitChangedFilesDiscoverer(repositoryPath));
            container.RegisterInstance(typeof(IAutomatedTestFramework<MSTestTestcase>), new MSTestFrameworkConnector(testAssemblies));
            container.RegisterType<IRTSApproach<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>, DocumentLevelDynamicRTSApproach>();

            container.RegisterType<OfflineController<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>>();

            var controller = container.Resolve<OfflineController<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>>();

		    var results = controller.ExecuteImpactedTests(new GitProgramVersion(VersionReferenceType.LatestCommit), new GitProgramVersion(VersionReferenceType.CurrentChanges));

            System.Console.WriteLine("Test Results:");

		    var testCaseResults = results as IList<ITestCaseResult<MSTestTestcase>> ?? results.ToList();
		    foreach (var result in testCaseResults)
		    {
		        System.Console.WriteLine($"{result.AssociatedTestCase.Id}: {result.Outcome}");
		    }
            int numberOfFailedTests = testCaseResults.Count(x => x.Outcome == TestCaseResultType.Failed);

            System.Console.WriteLine();
            System.Console.WriteLine(numberOfFailedTests == 0 ? "All tests passed!" : $"{numberOfFailedTests} of {testCaseResults.Count()} failed!" );

            System.Console.ReadKey();
		}
	}
}
