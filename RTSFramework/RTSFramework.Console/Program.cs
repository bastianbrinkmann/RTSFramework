using System.Diagnostics;
using System.IO;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Controller;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Artefacts;
using Unity;

namespace RTSFramework.Console
{
    class Program
    {
        private static void SetConfig<T>(RunConfiguration<T> configuration) where T : IProgramModel
        {
            configuration.ProcessingType = ProcessingType.MSTestExecutionWithCoverage;
            configuration.DiscoveryType = DiscoveryType.UserIntendedChangesDiscovery;
            configuration.GitRepositoryPath = @"C:\Git\TIATestProject";
            configuration.TestAssemblyFolders = new[] { @"C:\Git\TIATestProject\MainProject.Test\bin\Debug\" };
            configuration.RTSApproachType = RTSApproachType.DynamicRTS;
        }

        static void Main(string[] args)
        {
            UnityProvider.Initialize();

            //GitExampleRun();
            TFS2010ExampleRun();

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

            var controller = UnityProvider.GetTfs2010Controller();

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

            var controller = UnityProvider.GetGitController();

            controller.ExecuteImpactedTests(configuration);
        }

        
    }
}
