using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;
using RTSFramework.RTSApproaches.Concrete;
using Unity;
using Unity.Lifetime;
using Unity.Policy.Mapping;

namespace RTSFramework.Console
{
	class Program
	{
		static void Main(string[] args)
		{
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            //string solutionFile = @"C:\Git\TIATestProject\TIATestProject.sln";
            string repositoryPath = @"C:\Git\TIATestProject";
            List<string> testAssemblies = new List<string>
            {
                @"C:\Git\TIATestProject\MainProject.Test\bin\Debug\MainProject.Test.dll"
            };

            var container = new UnityContainer();

		    container.RegisterInstance(typeof(IOfflineDeltaDiscoverer<GitProgramVersion, CSharpDocument, IDelta<CSharpDocument, GitProgramVersion>>), new LocalGitChangedFilesDiscoverer(repositoryPath));
            //container.RegisterInstance(typeof(IAutomatedTestFramework<MSTestTestcase>), new MSTestFrameworkConnector(testAssemblies));
            container.RegisterInstance(typeof(IAutomatedTestFramework<MSTestTestcase>), new MSTestFrameworkConnectorWithMapUpdating(testAssemblies));


            container.RegisterType<
                IRTSApproach<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>, 
                RetestAllApproach<IDelta<CSharpDocument, GitProgramVersion>, CSharpDocument, GitProgramVersion, MSTestTestcase>> ();
            
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

        static Assembly CurrentDomain_AssemblyResolve(object sender,ResolveEventArgs args)
        {
            var assemblyname = new AssemblyName(args.Name).Name;

            var assemblyFileName = Path.Combine(Environment.GetEnvironmentVariable("VS140COMNTOOLS"), assemblyname + ".dll");
            if (File.Exists(assemblyFileName))
            {
                var assembly = Assembly.LoadFrom(assemblyFileName);
                return assembly;
            }
            return null;
        }
    }
}
