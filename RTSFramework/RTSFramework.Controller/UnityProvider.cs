using System;
using RTSFramework.Concrete.Adatpers.DeltaAdapters;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Concrete.User;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Artefacts;
using RTSFramework.RTSApproaches.Concrete;
using RTSFramework.RTSApproaches.Utilities;
using Unity;
using Unity.Injection;

namespace RTSFramework.Controller
{
    public static class UnityProvider
    {
        private static IUnityContainer UnityContainer { get; } = new UnityContainer();

        public static void Initialize()
        {
            InitAdapters();
            InitDynamicMapHandling();

            InitDiscoverer();
            InitTestFramework();
            InitRTSApproaches();
            InitTestProcessors();

            InitController();
        }

        public static RTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase> GetTfs2010Controller()
        {
            return UnityContainer.Resolve<RTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
        }

        public static RTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase> GetGitController()
        {
            return UnityContainer.Resolve<RTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();
        }

        private static void InitDynamicMapHandling()
        {
            UnityContainer.RegisterType<IDynamicMapUpdater, DynamicMapUpdater>();
            UnityContainer.RegisterInstance(typeof(DynamicMapProvider), new DynamicMapProvider());
        }

        private static void InitTestProcessors()
        {
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, CsvTestsReporter<MSTestTestcase>>(ProcessingType.Reporting.ToString());
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestFrameworkConnectorWithOpenCoverage>(ProcessingType.MSTestExecutionWithCoverage.ToString());
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestFrameworkConnector>(ProcessingType.MSTestExecutionWithoutCoverage.ToString());

            UnityContainer.RegisterType<Func<ProcessingType, ITestProcessor<MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<ProcessingType, ITestProcessor<MSTestTestcase>>(name => c.Resolve<ITestProcessor<MSTestTestcase>>(name.ToString()))));
        }

        private static void InitRTSApproaches()
        {
            UnityContainer.RegisterType<IRTSApproach<CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<CSharpFileElement, MSTestTestcase>, RetestAllApproach<CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<CSharpFileElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<CSharpFileElement, MSTestTestcase>>(name.ToString()))));
        }

        private static void InitTestFramework()
        {
            //TODO Seperate TestFramework Part and Execution Part?
            UnityContainer.RegisterType<ITestFramework<MSTestTestcase>, MSTestFrameworkConnector>();
        }

        private static void InitDiscoverer()
        {
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, LocalGitChangedFilesDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));

            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));
        }

        private static void InitController()
        {
            UnityContainer.RegisterType<RTSController<FileElement, CSharpFileElement, GitProgramModel, MSTestTestcase>>();
            UnityContainer.RegisterType<RTSController<FileElement, CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
        }

        private static void InitAdapters()
        {
            //Trivial Adapters
            UnityContainer.RegisterType<IDeltaAdapter<FileElement, FileElement>, TrivialDeltaAdapter<FileElement>>();
            UnityContainer.RegisterType<IDeltaAdapter<CSharpFileElement, CSharpFileElement>, TrivialDeltaAdapter<CSharpFileElement>>();

            UnityContainer.RegisterType<IDeltaAdapter<FileElement, CSharpFileElement>, FileDeltaCSharpFileDeltaAdapter>();
        }
    }
}