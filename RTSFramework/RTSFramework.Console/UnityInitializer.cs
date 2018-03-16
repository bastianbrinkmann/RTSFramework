using System.Collections.Generic;
using RTSFramework.Concrete.Adatpers;
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
    public static class UnityInitializer
    {
        public static void Init(IUnityContainer container, string gitRepoPath = null)
        {
            InitAdapters(container);

            InitDiscoverer(container, gitRepoPath);
            InitTestFramework(container);
            InitRTSApproaches(container);
            InitController(container);
        }

        private static void InitRTSApproaches(IUnityContainer container)
        {
            //var rtsApproach = new RetestAllApproach<CSharpFileElement, MSTestTestcase>();
            var rtsApproach = new DynamicRTSApproach<CSharpFileElement, MSTestTestcase>();

            container.RegisterInstance(typeof(IRTSApproach<CSharpFileElement, MSTestTestcase>), rtsApproach);
        }

        private static void InitTestFramework(IUnityContainer container)
        {
            List<string> testAssemblies = new List<string>
            {
                @"C:\Git\TIATestProject\MainProject.Test\bin\Debug\MainProject.Test.dll"
            };

            //var testFramework = new MSTestFrameworkConnector(testAssemblies);
            var testFramework = new MSTestFrameworkConnectorWithOpenCoverage(testAssemblies);

            container.RegisterInstance(typeof(IAutomatedTestFramework<MSTestTestcase>), testFramework);
        }

        private static void InitDiscoverer(IUnityContainer container, string gitRepoPath)
        {             
            string[] intendedChanges = { @"C:\Git\TIATestProject\MainProject\Calculator.cs" };

            //var discoverer = new LocalGitChangedFilesDiscoverer(gitRepoPath);
            var discoverer = new UserIntendedChangesDiscoverer<GitProgramModel>(intendedChanges);

            container.RegisterInstance(typeof(IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>), discoverer);
        }

        private static void InitController(IUnityContainer container)
        {
            container.RegisterType<DynamicRTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();
        }

        private static void InitAdapters(IUnityContainer container)
        {
            //Trivial Adapters
            container.RegisterType<IDeltaAdapter<FileElement, FileElement>, TrivialDeltaAdapter<FileElement>>();
            container.RegisterType<IDeltaAdapter<CSharpFileElement, CSharpFileElement>, TrivialDeltaAdapter<CSharpFileElement>>();

            container.RegisterType<IDeltaAdapter<FileElement, CSharpFileElement>, FileDeltaCSharpFileDeltaAdapter>();
        }

    }
}