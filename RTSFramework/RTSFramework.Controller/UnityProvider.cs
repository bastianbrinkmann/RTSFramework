using System;
using System.IO;
using RTSFramework.Concrete.Adatpers.DeltaAdapters;
using RTSFramework.Concrete.CSharp;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Artefacts;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Artefacts;
using RTSFramework.Concrete.User;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Contracts.Delta;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Artefacts;
using RTSFramework.RTSApproaches.Concrete;
using RTSFramework.RTSApproaches.CorrespondenceModel;
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
            InitCorrespondenceModelManager();

            InitDiscoverer();
            InitTestsDiscoverer();
            InitRTSApproaches();
            InitTestsProcessors();

            InitController();
        }

        public static FileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase> GetTfs2010Controller()
        {
            return UnityContainer.Resolve<FileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
        }

        public static FileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase> GetGitController()
        {
            return UnityContainer.Resolve<FileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>>();
        }

        private static void InitCorrespondenceModelManager()
        {
            UnityContainer.RegisterInstance(typeof(CorrespondenceModelManager));
        }

        private static void InitTestsProcessors()
        {
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, CsvTestsReporter<MSTestTestcase>>(ProcessingType.Reporting.ToString());
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionWithCoverage.ToString());
            UnityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutor>(ProcessingType.MSTestExecutionWithoutCoverage.ToString());

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

        private static void InitTestsDiscoverer()
        {
            UnityContainer.RegisterType<ITestsDiscoverer<MSTestTestcase>, MSTestTestsDiscoverer>();
        }

        private static void InitDiscoverer()
        {
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, LocalGitFilesDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));

            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            //NestedDiscoverers
            UnityContainer.RegisterType<INestedOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>, StructuralDelta<FileElement>>, CSharpFilesDeltaDiscoverer<GitProgramModel>>();
            UnityContainer.RegisterType<INestedOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpFileElement>, StructuralDelta<FileElement>>, CSharpFilesDeltaDiscoverer<TFS2010ProgramModel>>();

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));
        }

        private static void InitController()
        {
            UnityContainer.RegisterType<FileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>>();
            UnityContainer.RegisterType<FileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
        }

        private static void InitAdapters()
        {
            //Trivial Adapters
            UnityContainer.RegisterType<IDeltaAdapter<FileElement, FileElement>, TrivialDeltaAdapter<FileElement>>();
            UnityContainer.RegisterType<IDeltaAdapter<CSharpFileElement, CSharpFileElement>, TrivialDeltaAdapter<CSharpFileElement>>();

            //Artefact Adapters
            UnityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
        }
    }
}