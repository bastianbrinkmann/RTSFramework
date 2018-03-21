using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Console.RunConfigurations;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;
using RTSFramework.Core.Artefacts;
using Unity;

namespace RTSFramework.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new RunConfiguration
            {
                ProcessingType = ProcessingType.Reporting,
                DiscoveryType = DiscoveryType.LocalDiscovery,
                ProgramModelType = ProgramModelType.GitProgramModel,
                GitRepositoryPath = @"C:\Git\TIATestProject",
                IntendedChanges = new[] {@"C:\Git\TIATestProject\MainProject\Calculator.cs"},
                TestAssemblyFolders = new[] {@"C:\Git\TIATestProject\MainProject.Test\bin\Debug\"},
                RTSApproachType = RTSApproachType.DynamicRTS,
                PersistDynamicMap = true
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

            var controller = container.Resolve<RTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();

            controller.ExecuteImpactedTests(oldProgramModel, newProgramModel);
        }

        private static void GitExampleRun(IUnityContainer container, RunConfiguration configuration)
        {
            var oldProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.LatestCommit);
            var newProgramModel = GitProgramModelProvider.GetGitProgramModel(configuration.GitRepositoryPath,
                GitVersionReferenceType.CurrentChanges);

            var controller = container.Resolve<RTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();

            controller.ExecuteImpactedTests(oldProgramModel, newProgramModel);
        }
    }
}
