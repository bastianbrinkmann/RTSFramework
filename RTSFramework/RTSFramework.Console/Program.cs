using System.Diagnostics;
using System.IO;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Contracts.Models;
using RTSFramework.Controller;
using RTSFramework.Controller.RunConfigurations;
using Unity;

namespace RTSFramework.Console
{
    class Program
    {
        private static void SetConfig<T>(RunConfiguration<T> configuration) where T : CSharpProgramModel
        {
            configuration.ProcessingType = ProcessingType.MSTestExecutionWithCoverage;
            configuration.DiscoveryType = DiscoveryType.UserIntendedChangesDiscovery;
            configuration.GitRepositoryPath = @"C:\Git\TIATestProject\";
            configuration.AbsoluteSolutionPath = @"C:\Git\TIATestProject\TIATestProject.sln";
            configuration.RTSApproachType = RTSApproachType.DynamicRTS;
            configuration.GranularityLevel = GranularityLevel.File;
        }

        static void Main(string[] args)
        {
            UnityProvider.Initialize();

            GitExampleRun();
            //TFS2010ExampleRun();

            Process.Start(Directory.GetCurrentDirectory());
        }

        private static void TFS2010ExampleRun()
        {
            var configuration = new RunConfiguration<TFS2010ProgramModel>();
            SetConfig(configuration);

            var oldProgramModel = new TFS2010ProgramModel
            {
                VersionId = "Test"
            };
            var newProgramModel = new TFS2010ProgramModel
            {
                VersionId = "Test2"
            };
            configuration.OldProgramModel = oldProgramModel;
            configuration.NewProgramModel = newProgramModel;
            configuration.OldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
            configuration.NewProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;

            var controller = UnityProvider.GetTfs2010CSharpFileController();

            controller.ExecuteImpactedTests(configuration);
        }

        private static void GitExampleRun()
        {
            var configuration = new RunConfiguration<GitProgramModel>();
            SetConfig(configuration);

            var oldProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.LatestCommit);
            var newProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.CurrentChanges);
            configuration.OldProgramModel = oldProgramModel;
            configuration.NewProgramModel = newProgramModel;
            configuration.OldProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;
            configuration.NewProgramModel.AbsoluteSolutionPath = configuration.AbsoluteSolutionPath;

            if (configuration.GranularityLevel == GranularityLevel.File)
            {
                var controller = UnityProvider.GetGitCSharpFileController();
                controller.ExecuteImpactedTests(configuration);
            }
            else
            {
                var controller = UnityProvider.GetGitCSharpClassController();
                controller.ExecuteImpactedTests(configuration);
            }
        }

        
    }
}
