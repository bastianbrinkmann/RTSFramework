using System;
using System.IO;
using RTSFramework.Concrete.Adatpers.DeltaAdapters;
using RTSFramework.Concrete.CSharp.Core;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Concrete.User;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.RTSApproach;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.Concrete;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace RTSFramework.Controller
{
    public static class UnityProvider
    {
        private static IUnityContainer UnityContainer { get; } = new UnityContainer();

        public static void Initialize()
        {
            InitAdapters();
            InitDeletedFilesProvider();

            InitCorrespondenceModelManager();

            InitDiscoverer();
            InitTestsDiscoverer();
            InitRTSApproaches();
            InitTestsProcessors();

            InitController();

        }

        public static FileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase> GetTfs2010CSharpFileController()
        {
            return UnityContainer.Resolve<FileRTSController<CSharpFileElement, TFS2010ProgramModel, MSTestTestcase>>();
        }

        public static FileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase> GetGitCSharpFileController()
        {
            return UnityContainer.Resolve<FileRTSController<CSharpFileElement, GitProgramModel, MSTestTestcase>>();
        }

        public static FileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase> GetGitCSharpClassController()
        {
            return UnityContainer.Resolve<FileRTSController<CSharpClassElement, GitProgramModel, MSTestTestcase>>();
        }

        private static void InitDeletedFilesProvider()
        {
            UnityContainer.RegisterType<IFilesProvider<GitProgramModel>, GitFilesProvider>();
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
            //FileLevel
            UnityContainer.RegisterType<IRTSApproach<CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<CSharpFileElement, MSTestTestcase>, RetestAllApproach<CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<CSharpFileElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<CSharpFileElement, MSTestTestcase>>(name.ToString()))));

            //ClassLevel
            UnityContainer.RegisterType<IRTSApproach<CSharpClassElement, MSTestTestcase>, DynamicRTSApproach<CSharpClassElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<CSharpClassElement, MSTestTestcase>, RetestAllApproach<CSharpClassElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<CSharpClassElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<CSharpClassElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<CSharpClassElement, MSTestTestcase>>(name.ToString()))));
        }

        private static void InitTestsDiscoverer()
        {
            UnityContainer.RegisterType<ITestsDiscoverer<MSTestTestcase>, MSTestTestsDiscoverer>();
        }

        private static void InitDiscoverer()
        {
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, LocalGitFilesDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>, UserIntendedChangesDiscoverer<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            //NestedDiscoverers
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>, CSharpFilesDeltaDiscoverer<GitProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpFileElement>>, CSharpFilesDeltaDiscoverer<TFS2010ProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpClassElement>>, CSharpClassDeltaDiscoverer<GitProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpClassElement>>, CSharpClassDeltaDiscoverer<TFS2010ProgramModel>>();

            InitDiscovererFactories();
        }

        private static void InitDiscovererFactories()
        {
            UnityContainer.RegisterType<Action<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpFileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpFileElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpFileElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpClassElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpClassElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<CSharpClassElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>>(
               new InjectionFactory(c =>
               new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>(name.ToString()))));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<FileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpClassElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpClassElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpFileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<CSharpClassElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
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
            UnityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
            UnityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
        }
    }
}