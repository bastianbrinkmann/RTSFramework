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
using RTSFramework.Concrete.TFS2010;
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
using Unity.Lifetime;
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

			InitDeltaDiscoverer(unityContainer);
			InitTestsDiscoverer(unityContainer);
			InitRTSApproaches(unityContainer);
			InitTestsProcessors(unityContainer);
		}

		private static void InitHelper(IUnityContainer unityContainer)
		{
			//FilesProvider
			unityContainer.RegisterType<IFilesProvider<GitProgramModel>, GitFilesProvider>();
			unityContainer.RegisterType<IFilesProvider<TFS2010ProgramModel>, LocalFilesProvider>();

			unityContainer.RegisterType<IntertypeRelationGraphBuilder>();
		}

		private static void InitCorrespondenceModelManager(IUnityContainer unityContainer)
		{
			unityContainer.RegisterInstance(typeof(CorrespondenceModelManager));
		}

		#region TestDiscoverer

		private static void InitTestsDiscoverer(IUnityContainer unityContainer)
		{
			InitTestsDiscovererForModel<GitProgramModel>(unityContainer);
			InitTestsDiscovererForModel<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitTestsDiscovererForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<ITestsDiscoverer<TModel, MSTestTestcase>, MSTestTestsDiscoverer<TModel>>();
		}

		#endregion

		#region DeltaDiscoverer
		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, LocalGitFileDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
			InitDiscovererForModels<GitProgramModel>(unityContainer);
			InitDiscovererForModels<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitDiscovererForModels<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>>, UserIntendedChangesDiscoverer<TModel>>(DiscoveryType.UserIntendedChangesDiscovery.ToString());
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>, CSharpFilesDeltaDiscoverer<TModel>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpClassElement>>, CSharpClassDeltaDiscoverer<TModel>>();

			InitDiscovererFactories<TModel>(unityContainer);
		}

		private static void InitDiscovererFactories<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			//FileElement Discoverers
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>>>(name => c.Resolve<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>>>(name.ToString()))));

			//CSharpFileElement Discoverers
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>>(discoveryType =>
				{
					var fileElementFactory = c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, FileElement>>>>();
					var fileElementDiscoverer = fileElementFactory(discoveryType);

					return c.Resolve<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>>(new ParameterOverride("internalDiscoverer", fileElementDiscoverer));
				})));

			//CSharpClassElement Discoverers
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpClassElement>>>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpClassElement>>>(discoveryType =>
				{
					var cSharpFileElementFactory = c.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>>>>();
					var cSharpFileElementDiscoverer = cSharpFileElementFactory(discoveryType);

					return c.Resolve<IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpClassElement>>>(new ParameterOverride("internalDiscoverer", cSharpFileElementDiscoverer));
				})));
		}

		#endregion

		#region RTSApproaches

		private static void InitRTSApproaches(IUnityContainer unityContainer)
		{
			InitRTSApproachesForModel<GitProgramModel>(unityContainer);
			InitRTSApproachesForModel<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitRTSApproachesForModel<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			unityContainer.RegisterType<IRTSApproach<StructuralDelta<TModel, CSharpClassElement>, MSTestTestcase>, ClassSRTSApproach<TModel>>(RTSApproachType.ClassSRTS.ToString());
			InitRTSAproachesForModelAndElementType<TModel, CSharpFileElement>(unityContainer);
			InitRTSAproachesForModelAndElementType<TModel, CSharpClassElement>(unityContainer);
		}

		private static void InitRTSAproachesForModelAndElementType<TModel, TModelElement>(IUnityContainer unityContainer) where TModel : IProgramModel where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<IRTSApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>, DynamicRTSApproach<TModel, TModelElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>, RetestAllApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name => c.Resolve<IRTSApproach<StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name.ToString()))));
		}

		#endregion

		private static void InitTestsProcessors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, CsvTestsReporter<MSTestTestcase>>(ProcessingType.CsvReporting.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionWithCoverage.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, MSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase>, TestCaseListReporter<MSTestTestcase>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<Func<ProcessingType, ITestProcessor<MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestProcessor<MSTestTestcase>>(name => c.Resolve<ITestProcessor<MSTestTestcase>>(name.ToString()))));
		}

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();

			//Cancelable Adapter
			unityContainer.RegisterType<CancelableArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
		}
	}
}