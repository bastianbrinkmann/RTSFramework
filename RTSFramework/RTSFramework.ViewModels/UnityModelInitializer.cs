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
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.ClassSRTS;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.ViewModels.RunConfigurations;
using Unity;
using Unity.Injection;
using Unity.Resolution;

namespace RTSFramework.ViewModels
{
	public static class UnityModelInitializer
	{
		public static void InitializeModelClasses(IUnityContainer unityContainer)
		{
			InitAdapters(unityContainer);
			InitHelper(unityContainer);

			InitCorrespondenceModelManager(unityContainer);

			InitDiscoverer(unityContainer);
			InitTestsDiscoverer(unityContainer);
			InitRTSApproaches(unityContainer);
			InitTestsProcessors(unityContainer);
		}

		private static void InitHelper(IUnityContainer unityContainer)
		{
			//FilesProvider
			unityContainer.RegisterType<IFilesProvider<GitProgramModel>, GitFilesProvider>(DiscoveryType.LocalDiscovery.ToString());
			unityContainer.RegisterType<IFilesProvider<GitProgramModel>, LocalFilesProvider<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
			unityContainer.RegisterType<IFilesProvider<TFS2010ProgramModel>, LocalFilesProvider<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

			//FilesProviderFactory
			InitFileProdiverFactories<GitProgramModel>(unityContainer);
			InitFileProdiverFactories<TFS2010ProgramModel>(unityContainer);

			unityContainer.RegisterType<IntertypeRelationGraphBuilder>();
		}

		private static void InitFileProdiverFactories<TP>(IUnityContainer unityContainer) where TP : IProgramModel
		{
			unityContainer.RegisterType<Func<DiscoveryType, IFilesProvider<TP>>>(
			   new InjectionFactory(c =>
			   new Func<DiscoveryType, IFilesProvider<TP>>(name => c.Resolve<IFilesProvider<TP>>(name.ToString()))));
		}

		private static void InitCorrespondenceModelManager(IUnityContainer unityContainer)
		{
			unityContainer.RegisterInstance(typeof(CorrespondenceModelManager));
		}

		private static void InitTestsProcessors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, CsvTestsReporter<MSTestTestcase>>(ProcessingType.CsvReporting.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionWithCoverage.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());

			unityContainer.RegisterType<Func<ProcessingType, ITestProcessor<MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestProcessor<MSTestTestcase>>(name => c.Resolve<ITestProcessor<MSTestTestcase>>(name.ToString()))));
		}

		private static void InitRTSApproaches(IUnityContainer unityContainer)
		{
			//Git
			//FileLevel
			unityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>, RetestAllApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<GitProgramModel, CSharpFileElement, MSTestTestcase>>(name.ToString()))));

			//ClassLevel
			unityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, DynamicRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, RetestAllApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
			unityContainer.RegisterType<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>, ClassSRTSApproach<GitProgramModel>>(RTSApproachType.ClassSRTS.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<GitProgramModel, CSharpClassElement, MSTestTestcase>>(name.ToString()))));

			//TFS2010
			//FileLevel
			unityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>, DynamicRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>, RetestAllApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<TFS2010ProgramModel, CSharpFileElement, MSTestTestcase>>(name.ToString()))));

			//ClassLevel
			unityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, DynamicRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, RetestAllApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
			unityContainer.RegisterType<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>, ClassSRTSApproach<TFS2010ProgramModel>>(RTSApproachType.ClassSRTS.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(name => c.Resolve<IRTSApproach<TFS2010ProgramModel, CSharpClassElement, MSTestTestcase>>(name.ToString()))));
		}

		private static void InitTestsDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestsDiscoverer<MSTestTestcase>, MSTestTestsDiscoverer>();
		}

		private static void InitDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, LocalGitFilesDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, UserIntendedChangesDiscoverer<GitProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>, UserIntendedChangesDiscoverer<TFS2010ProgramModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

			//NestedDiscoverers
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>, CSharpFilesDeltaDiscoverer<GitProgramModel>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>, CSharpFilesDeltaDiscoverer<TFS2010ProgramModel>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>, CSharpClassDeltaDiscoverer<GitProgramModel>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>, CSharpClassDeltaDiscoverer<TFS2010ProgramModel>>();

			InitDiscovererFactories(unityContainer);
		}

		private static void InitDiscovererFactories(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>(name.ToString()))));
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>(name =>
				{
					var fileDeltaDiscovererFactory =
						c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, FileElement>>>>();
					var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

					return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>(
						new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
				})));
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>(name =>
				{
					var fileDeltaDiscovererFactory =
						c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpFileElement>>>>();
					var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

					return c.Resolve<IOfflineDeltaDiscoverer<TFS2010ProgramModel, StructuralDelta<TFS2010ProgramModel, CSharpClassElement>>>(
						new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
				})));

			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>>(
			   new InjectionFactory(c =>
			   new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>(name.ToString()))));
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>(name =>
				{
					var fileDeltaDiscovererFactory =
						c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>>>();
					var fileDeltaDiscoverer = fileDeltaDiscovererFactory(name);

					return c.Resolve<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>>(
						new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
				})));
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>>>(
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

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
		}
	}
}