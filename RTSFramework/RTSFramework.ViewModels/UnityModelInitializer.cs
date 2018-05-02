using System;
using System.Collections.Generic;
using System.IO;
using RTSFramework.Concrete.CSharp.Core;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
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
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using RTSFramework.RTSApproaches.Static;
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

			InitDataStructureProvider(unityContainer);

			InitDeltaDiscoverer(unityContainer);
			InitTestsDiscoverer(unityContainer);
			InitTestSelectors(unityContainer);
			InitTestProcessors(unityContainer);
			InitTestInstrumentors(unityContainer);

			InitStateBasedController(unityContainer);

			container = unityContainer;
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
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta, ITestExecutionResult<MSTestTestcase>>(unityContainer);
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
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestSelector<TModel, TDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TDelta, TModel>>>();

							return unityContainer.Resolve<StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult>>(
								new ParameterOverride("deltaDiscoverer", deltaDiscovererFactory(discoveryType)),
								new ParameterOverride("testSelector", rtsApproachFactory(rtsApproachType)),
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
			//unityContainer.RegisterType<ITestsDiscoverer<TModel, MSTestTestcase>, MonoMSTestTestsDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestsDiscoverer<TModel, MSTestTestcase>, InProcessMSTestTestsDiscoverer<TModel>>();
		}

		#endregion

		#region DeltaDiscoverer
		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, GitFileDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, GitFileDeltaDiscoverer>(DiscoveryType.VersionCompare.ToString());
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

		private static void InitTestSelectors(IUnityContainer unityContainer)
		{
			InitTestSelectorsForCSharpModel<GitProgramModel>(unityContainer);
			InitTestSelectorsForCSharpModel<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitTestSelectorsForCSharpModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, MSTestTestcase>, ClassSRTSDeltaExpander<TModel>>(RTSApproachType.ClassSRTS.ToString());


			InitTestSelectorsForModelAndElementType<TModel, CSharpFileElement>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpClassElement>(unityContainer);
		}

		private static void InitTestSelectorsForModelAndElementType<TModel, TModelElement>(IUnityContainer unityContainer) where TModel : IProgramModel where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>, DynamicRTS<TModel, TModelElement, MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>, RetestAllSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name => c.Resolve<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, MSTestTestcase>>(name.ToString()))));
		}

		#endregion

		#region TestProcessors

		private static void InitTestProcessors(IUnityContainer unityContainer)
		{
			InitTestProcessors<GitProgramModel>(unityContainer);
			InitTestProcessors<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitTestProcessors<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			InitTestProcessors<StructuralDelta<TModel, CSharpFileElement>, TModel>(unityContainer);
			InitTestProcessors<StructuralDelta<TModel, CSharpClassElement>, TModel>(unityContainer);
		}

		private static void InitTestProcessors<TDelta, TModel>(IUnityContainer unityContainer) where TDelta : IDelta<TModel> where TModel : IProgramModel
		{
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, FileProcessingResult, TDelta, TModel>, CsvTestsReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString());

			//unityContainer.RegisterType<ITestProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestExecutionResult<MSTestTestcase>, TDelta, TModel>, TestExecutorWithInstrumenting<TModel, TDelta, MSTestTestcase>>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());

			//unityContainer.RegisterType<ITestProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestExecutionResult<MSTestTestcase>, TDelta, TModel>, InProcessMSTestTestsExecutor<TDelta, TModel>>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestExecutor<MSTestTestcase, TDelta, TModel>, InProcessMSTestTestsExecutor<TDelta, TModel>>();

			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, IdentifiedTestsListReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());

			InitTestProcessorsFactoryForResultType<FileProcessingResult, TDelta, TModel>(unityContainer);
			InitTestProcessorsFactoryForResultType<ITestExecutionResult<MSTestTestcase>, TDelta, TModel>(unityContainer);
			InitTestProcessorsFactoryForResultType<TestListResult<MSTestTestcase>, TDelta, TModel>(unityContainer);
		}

		private static void InitTestProcessorsFactoryForResultType<TResult, TDelta, TModel>(IUnityContainer unityContainer) where TResult : ITestProcessingResult where TDelta : IDelta<TModel> where TModel : IProgramModel
		{
			unityContainer.RegisterType<Func<ProcessingType, ITestProcessor<MSTestTestcase, TResult, TDelta, TModel>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestProcessor<MSTestTestcase, TResult, TDelta, TModel>>(name => c.Resolve<ITestProcessor<MSTestTestcase, TResult, TDelta, TModel>>(name.ToString()))));
		}
		#endregion

		#region TestInstrumentor

		private static void InitTestInstrumentors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestInstrumentor<GitProgramModel, MSTestTestcase>, MSTestInstrumentor<GitProgramModel>>();
			unityContainer.RegisterType<ITestInstrumentor<TFS2010ProgramModel, MSTestTestcase>, MSTestInstrumentor<TFS2010ProgramModel>>();
		}

		#endregion

		#region DataStructureProvider

		private static void InitDataStructureProvider(IUnityContainer unityContainer)
		{
			InitDataStructureProviderForModel<GitProgramModel>(unityContainer);
			InitDataStructureProviderForModel<TFS2010ProgramModel>(unityContainer);
		}

		private static void InitDataStructureProviderForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<IDataStructureProvider<IntertypeRelationGraph, TModel>, MonoIntertypeRelationGraphBuilder<TModel>>();
			//unityContainer.RegisterType<IDataStructureProvider<IntertypeRelationGraph, TModel>, RoslynCompiledIntertypeRelationGraphBuilder<TModel>>();
			

			unityContainer.RegisterType<IDataStructureProvider<CorrespondenceModel, TModel>, CorrespondenceModelManager<TModel>>(new ContainerControlledLifetimeManager());
		}

		#endregion

		private static void InitHelper(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IIntendedChangesProvider, IntendedFileChangesProvider>(new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<InProcessVsTestConnector>(new ContainerControlledLifetimeManager());
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