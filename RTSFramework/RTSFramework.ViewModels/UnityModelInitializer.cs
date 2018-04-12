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
using RTSFramework.ViewModels.Controller;
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
			InitTestProcessors(unityContainer);

			InitStateBasedController(unityContainer);
			InitDeltaBasedController(unityContainer);

			container = unityContainer;
		}

		internal static DeltaBasedController<TModel, TDelta, TTestCase, TResult> GetDeltaBasedController<TModel, TDelta, TTestCase, TResult>
			(RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, DeltaBasedController<TModel, TDelta, TTestCase, TResult>>>();

			return factory(rtsApproachType, processingType);
		}

		private static IUnityContainer container;
		internal static StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult> GetStateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>
			(DiscoveryType discoveryType, RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<DiscoveryType, RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>>>();

			return factory(discoveryType, rtsApproachType, processingType);
		}

		#region DeltaBasedController

		private static void InitDeltaBasedController(IUnityContainer unityContainer)
		{
			InitDeltaBasedControllerFactoryDelta<GitProgramModel>(unityContainer);
			InitDeltaBasedControllerFactoryDelta<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryDelta<TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitDeltaBasedControllerFactoryResult<TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitDeltaBasedControllerFactoryResult<TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryResult<TModel, TDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			InitDeltaBasedControllerFactoryTestcase<TModel, TDelta, MSTestExectionResult>(unityContainer);
			InitDeltaBasedControllerFactoryTestcase<TModel, TDelta, FileProcessingResult>(unityContainer);
			InitDeltaBasedControllerFactoryTestcase<TModel, TDelta, TestListResult<MSTestTestcase>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryTestcase<TModel, TDelta, TResult>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			InitDeltaBasedControllerFactory<TModel, TDelta, MSTestTestcase, TResult>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactory<TModel, TDelta, TTestCase, TResult>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, DeltaBasedController<TModel, TDelta, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, DeltaBasedController<TModel, TDelta, TTestCase, TResult>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, IRTSApproach<TModel, TDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult>>>();

							return unityContainer.Resolve<DeltaBasedController<TModel, TDelta, TTestCase, TResult>>(
								new ParameterOverride("rtsApproach", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testProcessor", testProcessorFactory(processingType)));
						})));
		}

		#endregion

		#region StateBasedController

		private static void InitStateBasedController(IUnityContainer unityContainer)
		{
			InitStateBasedControllerFactoryDelta<GitVersionIdentification, GitProgramModel>(unityContainer);
			InitStateBasedControllerFactoryDelta<TFS2010VersionIdentification, TFS2010ProgramModel>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryDelta<TArtefact, TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitStateBasedControllerFactoryResult<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitStateBasedControllerFactoryResult<TArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryResult<TArtefact, TModel, TDelta>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDelta : IDelta<TModel>
		{
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta, MSTestExectionResult>(unityContainer);
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta, FileProcessingResult>(unityContainer);
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta, TestListResult<MSTestTestcase>>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta, TResult>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			InitStateBasedControllerFactory<TArtefact, TModel, TDelta, MSTestTestcase, TResult>(unityContainer);
		}

		private static void InitStateBasedControllerFactory<TArtefact, TModel, TDelta, TTestCase, TResult>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDelta : IDelta<TModel>
			where TTestCase : ITestCase 
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<DiscoveryType, RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<DiscoveryType, RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>>(
						(discoveryType, rtsApproachType, processingType) =>
						{
							var deltaDiscovererFactory = unityContainer.Resolve<Func<DiscoveryType, IOfflineDeltaDiscoverer<TModel, TDelta>>>();
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, IRTSApproach<TModel, TDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult>>>();

							return unityContainer.Resolve<StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>>(
								new ParameterOverride("deltaDiscoverer", deltaDiscovererFactory(discoveryType)),
								new ParameterOverride("rtsApproach", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testProcessor", testProcessorFactory(processingType)));
						})));
		}

		#endregion

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
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, LocalGitFileDeltaDiscoverer>(DiscoveryType.VersionCompare.ToString());
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
			InitRTSApproachesForCSharpModel<GitProgramModel>(unityContainer);
			InitRTSApproachesForCSharpModel<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitRTSApproachesForCSharpModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<IRTSApproach<TModel, StructuralDelta<TModel, CSharpClassElement>, MSTestTestcase>, ClassSRTSApproach<TModel>>(RTSApproachType.ClassSRTS.ToString());
			InitRTSAproachesForModelAndElementType<TModel, CSharpFileElement>(unityContainer);
			InitRTSAproachesForModelAndElementType<TModel, CSharpClassElement>(unityContainer);
		}

		private static void InitRTSAproachesForModelAndElementType<TModel, TModelElement>(IUnityContainer unityContainer) where TModel : IProgramModel where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>, DynamicRTSApproach<TModel, TModelElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>, RetestAllApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name => c.Resolve<IRTSApproach<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name.ToString()))));
		}

		#endregion

		#region TestProcessors

		private static void InitTestProcessors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, FileProcessingResult>, CsvTestsReporter<MSTestTestcase>>(ProcessingType.CsvReporting.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, MSTestExectionResult>, MSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionWithCoverage.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, MSTestExectionResult>, MSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>>, IdentifiedTestsListReporter<MSTestTestcase>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());

			InitTestProcessorsFactoryForResultType<FileProcessingResult>(unityContainer);
			InitTestProcessorsFactoryForResultType<MSTestExectionResult>(unityContainer);
			InitTestProcessorsFactoryForResultType<TestListResult<MSTestTestcase>>(unityContainer);
		}

		private static void InitTestProcessorsFactoryForResultType<TResult>(IUnityContainer unityContainer) where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<ProcessingType, ITestProcessor<MSTestTestcase, TResult>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestProcessor<MSTestTestcase, TResult>>(name => c.Resolve<ITestProcessor<MSTestTestcase, TResult>>(name.ToString()))));
		}

		#endregion

		private static void InitHelper(IUnityContainer unityContainer)
		{
			//FilesProvider
			unityContainer.RegisterType<IFilesProvider<GitProgramModel>, GitFilesProvider>();
			unityContainer.RegisterType<IFilesProvider<TFS2010ProgramModel>, LocalFilesProvider>();

			unityContainer.RegisterType<IntertypeRelationGraphBuilder>();

			unityContainer.RegisterType<IIntendedChangesProvider, IntendedFileChangesProvider>(new ContainerControlledLifetimeManager());
		}

		private static void InitCorrespondenceModelManager(IUnityContainer unityContainer)
		{
			unityContainer.RegisterInstance(typeof(CorrespondenceModelManager));
		}

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<GitVersionIdentification, GitProgramModel>, GitProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<TFS2010VersionIdentification, TFS2010ProgramModel>, TFS2010ProgramModelAdapter>();

			//Cancelable Adapter
			unityContainer.RegisterType<CancelableArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
		}
	}
}