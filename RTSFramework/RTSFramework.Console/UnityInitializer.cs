using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.Adatpers.DeltaAdapters;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Concrete.User;
using RTSFramework.Console.RunConfigurations;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Core;
using RTSFramework.Core.Artefacts;
using RTSFramework.RTSApproaches.Concrete;
using Unity;

namespace RTSFramework.Console
{
    public static class UnityInitializer
    {
        public static void Init(IUnityContainer container, RunConfiguration configuration)
        {
            InitAdapters(container);
            InitReporting(container, configuration);
            InitDiscoverer(container, configuration);
            InitTestFramework(container, configuration);
            InitRTSApproaches(container, configuration);
            InitController(container, configuration);
        }

        private static void InitReporting(IUnityContainer container, RunConfiguration configuration)
        {
            if (configuration.ProcessingType == ProcessingType.Reporting)
            {
                container.RegisterType<ITestProcessor<MSTestTestcase>, CsvTestsReporter<MSTestTestcase>>();
            }
        }

        private static void InitRTSApproaches(IUnityContainer container, RunConfiguration configuration)
        {
            IRTSApproach<CSharpFileElement, MSTestTestcase> rtsApproach = null;

            if (configuration.RTSApproachType == RTSApproachType.RetestAll)
            {
                rtsApproach = new RetestAllApproach<CSharpFileElement, MSTestTestcase>();
            }
            else if (configuration.RTSApproachType == RTSApproachType.DynamicRTS)
            {
                rtsApproach = new DynamicRTSApproach<CSharpFileElement, MSTestTestcase>();
            }

            container.RegisterInstance(typeof(IRTSApproach<CSharpFileElement, MSTestTestcase>), rtsApproach);
        }

        private static void GetTestAssemblies(string folder, List<string> testAssemblies)
        {
            //TODO More advanced filtering for test assemblies?
            foreach (var assembly in Directory.GetFiles(folder, "*Test.dll"))
            {
                var fileName = Path.GetFileName(assembly);
                if (testAssemblies.All(x => !x.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    testAssemblies.Add(assembly);
                }
            }

            foreach (var subFolder in Directory.GetDirectories(folder))
            {
                GetTestAssemblies(subFolder, testAssemblies);
            }
        }

        private static void InitTestFramework(IUnityContainer container, RunConfiguration configuration)
        {
            IAutomatedTestFramework<MSTestTestcase> testFramework;

            var testAssemblies = new List<string>();

            foreach (var folder in configuration.TestAssemblyFolders)
            {
                GetTestAssemblies(folder, testAssemblies);
            }

            if (configuration.PersistDynamicMap)
            {
                testFramework = new MSTestFrameworkConnectorWithOpenCoverage(testAssemblies);
            }
            else
            {
                testFramework = new MSTestFrameworkConnector(testAssemblies);
            }

            container.RegisterInstance(typeof(ITestFramework<MSTestTestcase>), testFramework);

            if (configuration.ProcessingType == ProcessingType.Execution)
            {
                container.RegisterInstance(typeof(ITestProcessor<MSTestTestcase>), testFramework);
            }
        }

        private static void InitDiscoverer(IUnityContainer container, RunConfiguration configuration)
        {
            if (configuration.ProgramModelType == ProgramModelType.GitProgramModel)
            {
                IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>> gitDiscoverer = null;

                if (configuration.DiscoveryType == DiscoveryType.LocalDiscovery)
                {
                    gitDiscoverer = new LocalGitChangedFilesDiscoverer(configuration.GitRepositoryPath);
                }
                else if (configuration.DiscoveryType == DiscoveryType.UserBasedDiscovery)
                {
                    gitDiscoverer = new UserIntendedChangesDiscoverer<GitProgramModel>(configuration.IntendedChanges);
                }

                container.RegisterInstance(typeof(IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>), gitDiscoverer);
            }
            else if (configuration.ProgramModelType == ProgramModelType.TFS2010ProgramModel)
            {
                if (configuration.DiscoveryType == DiscoveryType.UserBasedDiscovery)
                {
                    var tfsDiscoverer = new UserIntendedChangesDiscoverer<TFS2010ProgramModel>(configuration.IntendedChanges);
                    container.RegisterInstance(typeof(IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>), tfsDiscoverer);
                }
            }
        }

        private static void InitController(IUnityContainer container, RunConfiguration configuration)
        {
            if (configuration.ProgramModelType == ProgramModelType.GitProgramModel)
            {
                container.RegisterType<RTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();
            }
            else if (configuration.ProgramModelType == ProgramModelType.TFS2010ProgramModel)
            {
                container.RegisterType<RTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
            }
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