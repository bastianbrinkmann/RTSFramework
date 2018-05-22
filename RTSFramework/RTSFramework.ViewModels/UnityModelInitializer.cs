using System;
using System.Collections;
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
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core;
using RTSFramework.Core.Models;
using RTSFramework.RTSApproaches.Dynamic;
using RTSFramework.RTSApproaches.Core;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;
using RTSFramework.RTSApproaches.CorrespondenceModel;
using RTSFramework.RTSApproaches.CorrespondenceModel.Models;
using RTSFramework.RTSApproaches.Static;
using RTSFramework.ViewModels.Adapter;
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
			InitTestsSelectors(unityContainer);
			InitTestsProcessors(unityContainer);
			InitTestsInstrumentors(unityContainer);
			InitTestsPrioritizers(unityContainer);

			InitStateBasedController(unityContainer);
			InitDeltaBasedController(unityContainer);

			container = unityContainer;
		}

		private static IUnityContainer container;
		internal static StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact> GetStateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>>();

			return factory(rtsApproachType, processingType);
		}

		internal static DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact> GetDeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>>();

			return factory(rtsApproachType, processingType);
		}

		private static void InitHelper(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IUserRunConfigurationProvider, UserRunConfigurationProvider>(new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<InProcessVsTestConnector>(new ContainerControlledLifetimeManager());
		}

		#region DeltaBasedController

		private static void InitDeltaBasedController(IUnityContainer unityContainer)
		{
			InitDeltaBasedControllerFactoryDelta<IntendedChangesArtefact, LocalProgramModel>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryDelta<TDeltaArtefact, TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TDelta, TTestCase>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, TDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TDelta, MSTestTestcase>(unityContainer);
			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestsSelector<TModel, TDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestsProcessor<TTestCase, TResult, TDelta, TModel>>>();

							return unityContainer.Resolve<DeltaBasedController<TDeltaArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>(
								new ParameterOverride("testsSelector", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testsProcessor", testProcessorFactory(processingType)));
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
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryResult<TArtefact, TModel, TDelta, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDelta : IDelta<TModel> where TTestCase : ITestCase
		{
			InitStateBasedControllerFactory<TArtefact, TModel, TDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitStateBasedControllerFactory<TArtefact, TModel, TDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDelta : IDelta<TModel>
		{
			InitStateBasedControllerFactory<TArtefact, TModel, TDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitStateBasedControllerFactoryResult<TArtefact, TModel, TDelta, MSTestTestcase>(unityContainer);
			InitStateBasedControllerFactoryResult<TArtefact, TModel, TDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitStateBasedControllerFactory<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDelta : IDelta<TModel>
			where TTestCase : ITestCase 
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestsSelector<TModel, TDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestsProcessor<TTestCase, TResult, TDelta, TModel>>>();

							return unityContainer.Resolve<StateBasedController<TArtefact, TModel, TDelta, TTestCase, TResult, TResultArtefact>>(
								new ParameterOverride("testsSelector", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testsProcessor", testProcessorFactory(processingType)));
						})));
		}

		#endregion

		#region TestsDiscoverer

		private static void InitTestsDiscoverer(IUnityContainer unityContainer)
		{
			InitTestsDiscovererForModel<GitProgramModel>(unityContainer);
			InitTestsDiscovererForModel<TFS2010ProgramModel>(unityContainer);
			InitTestsDiscovererForModel<LocalProgramModel>(unityContainer);
		}

		private static void InitTestsDiscovererForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			//unityContainer.RegisterType<ITestsDiscoverer<TModel, MSTestTestcase>, MonoMSTestTestsDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestsDiscoverer<TModel, MSTestTestcase>, InProcessMSTestTestsDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestsDiscoverer<TModel, CsvFileTestcase>, CsvTestsFileDiscoverer<TModel>>();
		}

		#endregion

		#region DeltaDiscoverer
		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, FileElement>>, GitFileDeltaDiscoverer<StructuralDelta<GitProgramModel, FileElement>>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpFileElement>>, GitFileDeltaDiscoverer<StructuralDelta<GitProgramModel, CSharpFileElement>>>();
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitProgramModel, StructuralDelta<GitProgramModel, CSharpClassElement>>, GitFileDeltaDiscoverer<StructuralDelta<GitProgramModel, CSharpClassElement>>>();

		}

		#endregion

		#region TestsSelectors

		private static void InitTestsSelectors(IUnityContainer unityContainer)
		{
			InitTestSelectorsForCSharpModel<GitProgramModel>(unityContainer);
			InitTestSelectorsForCSharpModel<TFS2010ProgramModel>(unityContainer);
			InitTestSelectorsForCSharpModel<LocalProgramModel>(unityContainer);
		}

		private static void InitTestSelectorsForCSharpModel<TModel>(IUnityContainer unityContainer)
			where TModel : CSharpProgramModel

		{
			InitTestSelectorsForTestType<TModel, MSTestTestcase>(unityContainer);
			InitTestSelectorsForTestType<TModel, CsvFileTestcase>(unityContainer);
		}

		private static void InitTestSelectorsForTestType<TModel, TTestCase>(IUnityContainer unityContainer) 
			where TModel : CSharpProgramModel
			where TTestCase : class, ITestCase
		{
			unityContainer.RegisterType<ITestsSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase>, ClassSRTSDeltaExpander<TModel, TTestCase>>(RTSApproachType.ClassSRTS.ToString());

			InitTestSelectorsForModelAndElementType<TModel, CSharpFileElement, TTestCase>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpClassElement, TTestCase>(unityContainer);
		}

		private static void InitTestSelectorsForModelAndElementType<TModel, TModelElement, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TModelElement : IProgramModelElement
			where TTestCase : class, ITestCase
		{
			unityContainer.RegisterType<ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, DynamicRTS<TModel, TModelElement, TTestCase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, RetestAllSelector<TModel, TModelElement, TTestCase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name => c.Resolve<ITestsSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name.ToString()))));
		}

		#endregion

		#region TestsProcessors

		private static void InitTestsProcessors(IUnityContainer unityContainer)
		{
			InitTestsProcessors<GitProgramModel>(unityContainer);
			InitTestsProcessors<TFS2010ProgramModel>(unityContainer);
			InitTestsProcessors<LocalProgramModel>(unityContainer);
		}

		private static void InitTestsProcessors<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			InitTestsProcessors<StructuralDelta<TModel, CSharpFileElement>, TModel>(unityContainer);
			InitTestsProcessors<StructuralDelta<TModel, CSharpClassElement>, TModel>(unityContainer);
		}

		private static void InitTestsProcessors<TDelta, TModel>(IUnityContainer unityContainer) where TDelta : IDelta<TModel> where TModel : IProgramModel
		{
			//unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutorWithOpenCoverage>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());
			unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, TestsExecutorWithInstrumenting<TModel, TDelta, MSTestTestcase>>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());

			//unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, InProcessMSTestTestsExecutor<TDelta, TModel>>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestsExecutor<MSTestTestcase, TDelta, TModel>, InProcessMSTestTestsExecutor<TDelta, TModel>>();

			unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, LimitedTimeTestsExecutor<MSTestTestcase, TDelta, TModel>>(ProcessingType.MSTestExecutionLimitedTime.ToString());

			unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestsReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestsReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<ITestsProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestsReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestsProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestsReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

			InitTestProcessorsFactoryForTestType<ITestsExecutionResult<MSTestTestcase>, TDelta, TModel, MSTestTestcase>(unityContainer);
			InitTestProcessorsFactoryForTestType<TestListResult<MSTestTestcase>, TDelta, TModel, MSTestTestcase>(unityContainer);
			InitTestProcessorsFactoryForTestType<TestListResult<CsvFileTestcase>, TDelta, TModel, CsvFileTestcase>(unityContainer);
		}

		private static void InitTestProcessorsFactoryForTestType<TResult, TDelta, TModel, TTestCase>(IUnityContainer unityContainer) 
			where TResult : ITestProcessingResult
			where TDelta : IDelta<TModel>
			where TModel : IProgramModel
			where TTestCase : ITestCase
		{
			unityContainer.RegisterType<Func<ProcessingType, ITestsProcessor<TTestCase, TResult, TDelta, TModel>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestsProcessor<TTestCase, TResult, TDelta, TModel>>(name => c.Resolve<ITestsProcessor<TTestCase, TResult, TDelta, TModel>>(name.ToString()))));
		}
		#endregion

		#region TestPrioritizer

		private static void InitTestsPrioritizers(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestsPrioritizer<MSTestTestcase>, NoOpPrioritizer<MSTestTestcase>>();
			unityContainer.RegisterType<ITestsPrioritizer<CsvFileTestcase>, NoOpPrioritizer<CsvFileTestcase>>();
		}

		#endregion

		#region TestInstrumentor

		private static void InitTestsInstrumentors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestsInstrumentor<GitProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<GitProgramModel>>();
			unityContainer.RegisterType<ITestsInstrumentor<TFS2010ProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<TFS2010ProgramModel>>();
			unityContainer.RegisterType<ITestsInstrumentor<LocalProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<LocalProgramModel>>();
		}

		#endregion

		#region DataStructureProvider

		private static void InitDataStructureProvider(IUnityContainer unityContainer)
		{
			InitDataStructureProviderForModel<GitProgramModel>(unityContainer);
			InitDataStructureProviderForModel<TFS2010ProgramModel>(unityContainer);
			InitDataStructureProviderForModel<LocalProgramModel>(unityContainer);
		}

		private static void InitDataStructureProviderForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<IDataStructureProvider<IntertypeRelationGraph, TModel>, MonoIntertypeRelationGraphBuilder<TModel>>();
			//unityContainer.RegisterType<IDataStructureProvider<IntertypeRelationGraph, TModel>, RoslynCompiledIntertypeRelationGraphBuilder<TModel>>();
			

			unityContainer.RegisterType<IDataStructureProvider<CorrespondenceModel, TModel>, CorrespondenceModelManager<TModel>>(new ContainerControlledLifetimeManager());
		}

		#endregion

		#region Adapters

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>, OpenCoverXmlCoverageAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<GitVersionIdentification, GitProgramModel>, GitProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<TFS2010VersionIdentification, TFS2010ProgramModel>, TFS2010ProgramModelAdapter>();

			//MSTest
			unityContainer.RegisterType<IArtefactAdapter<object, ITestsExecutionResult<MSTestTestcase>>, EmptyArtefactAdapter<ITestsExecutionResult<MSTestTestcase>>>();
			unityContainer.RegisterType<IArtefactAdapter<CsvFileArtefact, TestListResult<MSTestTestcase>>, TestsCsvFileAdapter<MSTestTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<IList<TestResultListViewItemViewModel>, TestListResult<MSTestTestcase>>, TestsResultListViewItemViewModelsAdapter<MSTestTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<TestResultListViewItemViewModel, MSTestTestcase>, TestResultListViewItemViewModelAdapter<MSTestTestcase>>();

			//CsvTest
			unityContainer.RegisterType<IArtefactAdapter<CsvFileArtefact, TestListResult<CsvFileTestcase>>, TestsCsvFileAdapter<CsvFileTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<IList<TestResultListViewItemViewModel>, TestListResult<CsvFileTestcase>>, TestsResultListViewItemViewModelsAdapter<CsvFileTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<TestResultListViewItemViewModel, CsvFileTestcase>, TestResultListViewItemViewModelAdapter<CsvFileTestcase>>();

			//Delta Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<LocalProgramModel, FileElement>>, IntendedChangesAdapter<StructuralDelta<LocalProgramModel, FileElement>>>();
			unityContainer.RegisterType<IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<LocalProgramModel, CSharpFileElement>>, IntendedChangesAdapter<StructuralDelta<LocalProgramModel, CSharpFileElement>>>();
			unityContainer.RegisterType<IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<LocalProgramModel, CSharpClassElement>>, IntendedChangesAdapter<StructuralDelta<LocalProgramModel, CSharpClassElement>>>();

			//Cancelable Adapter
			unityContainer.RegisterType<CancelableArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();

			//Delta Adapters
			InitDeltaAdaptersForModels<GitProgramModel>(unityContainer);
			InitDeltaAdaptersForModels<TFS2010ProgramModel>(unityContainer);
			InitDeltaAdaptersForModels<LocalProgramModel>(unityContainer);
		}

		private static void InitDeltaAdaptersForModels<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			InitDeltaAdaptersForModelElement<TModel, FileElement>(unityContainer);
			InitDeltaAdaptersForModelElement<TModel, CSharpFileElement>(unityContainer);
			InitDeltaAdaptersForModelElement<TModel, CSharpClassElement>(unityContainer);

			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<TModel, CSharpFileElement>, StructuralDelta<TModel, CSharpClassElement>, TModel>, CSharpFileClassDeltaAdapter<TModel>>();
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpFileElement>, TModel>, FilesCSharpFilesDeltaAdapter<TModel>>();
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpClassElement>, TModel>,
				ChainingDeltaAdapter<StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpFileElement>, StructuralDelta<TModel, CSharpClassElement>, TModel>>();
		}

		private static void InitDeltaAdaptersForModelElement<TModel, TModelElement>(IUnityContainer unityContainer) where TModel : IProgramModel
			where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<TModel, TModelElement>, StructuralDelta<TModel, TModelElement>, TModel>, IdentityDeltaAdapter<StructuralDelta<TModel, TModelElement>, TModel>>();
		}

		#endregion
	}
}