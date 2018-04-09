using System;
using System.Collections.Generic;
using System.IO;
using RTSFramework.Concrete.CSharp.Core;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn;
using RTSFramework.Concrete.CSharp.Roslyn.Adapters;
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
using RTSFramework.Controller;
using RTSFramework.Controller.RunConfigurations;
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.ClassSRTS;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using RTSFramework.RTSApproaches.Dynamic;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace RTSFramework.GUI.DependencyInjection
{
    internal static class UnityProvider
    {
        private static IUnityContainer UnityContainer { get; } = new UnityContainer();

        public static void Initialize()
        {
            InitAdapters();
            InitHelper();

            InitCorrespondenceModelManager();

            InitDiscoverer();
            InitTestsDiscoverer();
            InitRTSApproaches();
            InitTestsProcessors();

			GUIInitializer.InitializeGUI(UnityContainer);
        }

		internal static MainWindow GetMainWindow()
	    {
		    return UnityContainer.Resolve<MainWindow>();
	    }

        private static void InitHelper()
        {
			//FilesProvider
            UnityContainer.RegisterType<IFilesProvider<GitProgramModel>, GitFilesProvider>(DiscoveryType.LocalDiscovery.ToString());
			UnityContainer.RegisterType<IFilesProvider<GitProgramModel>, LocalFilesProvider<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
			UnityContainer.RegisterType<IFilesProvider<TFS2010ProgramModel>, LocalFilesProvider<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

			//FilesProviderFactory
	        InitFileProdiverFactories<GitProgramModel>();
			InitFileProdiverFactories<TFS2010ProgramModel>();

			UnityContainer.RegisterType<IntertypeRelationGraphBuilder>();
        }

	    private static void InitFileProdiverFactories<TP>() where TP : IProgramModel
	    {
			UnityContainer.RegisterType<Func<DiscoveryType, IFilesProvider<TP>>>(
			   new InjectionFactory(c =>
			   new Func<DiscoveryType, IFilesProvider<TP>>(name => c.Resolve<IFilesProvider<TP>>(name.ToString()))));
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
            //Git
            //FileLevel
            UnityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>, RetestAllApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
            
            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(name.ToString()))));

            //ClassLevel
            UnityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, DynamicRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, RetestAllApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
            UnityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, ClassSRTSApproach<GitProgramModel>>(RTSApproachType.ClassSRTS.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(name.ToString()))));

            //TFS2010
            //FileLevel
            UnityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>, RetestAllApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(name.ToString()))));

            //ClassLevel
            UnityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, DynamicRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
            UnityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, RetestAllApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
            UnityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, ClassSRTSApproach<TFS2010ProgramModel>>(RTSApproachType.ClassSRTS.ToString());

            UnityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>>(
                new InjectionFactory(c =>
                new Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(name.ToString()))));
        }

        private static void InitTestsDiscoverer()
        {
            UnityContainer.RegisterType<ITestsDiscoverer<MSTestTestcase>, MSTestTestsDiscoverer>();
        }

        private static void InitDiscoverer()
        {
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, LocalGitFilesDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, UserIntendedChangesDiscoverer<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>, UserIntendedChangesDiscoverer<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

            //NestedDiscoverers
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>, CSharpFilesDeltaDiscoverer<GitProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>, CSharpFilesDeltaDiscoverer<TFS2010ProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>, CSharpClassDeltaDiscoverer<GitProgramModel>>();
            UnityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>, CSharpClassDeltaDiscoverer<TFS2010ProgramModel>>();

            InitDiscovererFactories();
        }

        private static void InitDiscovererFactories()
        {
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>(name.ToString()))));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));

            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>>(
               new InjectionFactory(c =>
               new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>(name.ToString()))));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
            UnityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>>>(
                new InjectionFactory(c =>
                new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>>(name =>
                {
                    var fileDeltaDiscovererFactory =
                        c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>>();
                    var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

                    return c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>>(
                        new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
                })));
        }

        private static void InitAdapters()
        {
            //Artefact Adapters
            UnityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
            UnityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
            UnityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
            UnityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
        }
    }
}