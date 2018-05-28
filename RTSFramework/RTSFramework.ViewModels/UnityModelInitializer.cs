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

			InitModelLevelController(unityContainer);
			InitStateBasedController(unityContainer);
			InitDeltaBasedController(unityContainer);

			container = unityContainer;
		}

		private static IUnityContainer container;
		internal static StateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact> GetStateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDiscoveryDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>>>();

			return factory(rtsApproachType, processingType);
		}

		internal static DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact> GetDeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>>>();

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
			InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, StructuralDelta<TModel, FileElement> , StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, StructuralDelta<TModel, FileElement> , StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, MSTestTestcase>(unityContainer);
			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>>(
						(rtsApproachType, processingType) =>
						{
							var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult>>>();

							return unityContainer.Resolve<DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>>(
								new ParameterOverride("modelBasedController", modelLevelControllerFactory(rtsApproachType, processingType)));
						})));
		}

		#endregion

		#region StateBasedController

		private static void InitStateBasedController(IUnityContainer unityContainer)
		{
			InitStateBasedControllerFactoryDelta<GitVersionIdentification, GitCSharpProgramModel>(unityContainer);
			InitStateBasedControllerFactoryDelta<TFS2010VersionIdentification, TFS2010ProgramModel>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryDelta<TArtefact, TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitStateBasedControllerFactoryTestcase<TArtefact, TModel, StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryTestcase<TArtefact, TModel, TDeltaDisovery, TDeltaSelection>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
		{
			InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitStateBasedControllerFactoryResult<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, MSTestTestcase>(unityContainer);
			InitStateBasedControllerFactoryResult<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, CsvFileTestcase>(unityContainer);
		}

		private static void InitStateBasedControllerFactoryResult<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel> 
			where TTestCase : ITestCase
		{
			InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
		}

		private static void InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TTestCase : ITestCase 
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>>(
						(rtsApproachType, processingType) =>
						{
							var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>>();

							return unityContainer.Resolve<StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>>(
								new ParameterOverride("modelBasedController", modelLevelControllerFactory(rtsApproachType, processingType)));
						})));
		}

		#endregion

		#region ModelBasedController

		private static void InitModelLevelController(IUnityContainer unityContainer)
		{
			InitModelLevelControllerFactoryDelta<GitCSharpProgramModel>(unityContainer);
			InitModelLevelControllerFactoryDelta<TFS2010ProgramModel>(unityContainer);
			InitModelLevelControllerFactoryDelta<LocalProgramModel>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryDelta<TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitModelLevelControllerFactoryTestcase<TModel, StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitModelLevelControllerFactoryTestcase<TModel, StructuralDelta<TModel, FileElement>, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryTestcase<TModel, TDeltaDisovery, TDeltaSelection>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
		{
			InitModelLevelControllerFactory<TModel, TDeltaDisovery, TDeltaSelection, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>>(unityContainer);

			InitModelLevelControllerFactoryResult<TModel, TDeltaDisovery, TDeltaSelection, MSTestTestcase>(unityContainer);
			InitModelLevelControllerFactoryResult<TModel, TDeltaDisovery, TDeltaSelection, CsvFileTestcase>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryResult<TModel, TDeltaDisovery, TDeltaSelection, TTestCase>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitModelLevelControllerFactory<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TestListResult<TTestCase>>(unityContainer);
		}

		private static void InitModelLevelControllerFactory<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestSelector<TModel, TDeltaSelection, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TDeltaSelection, TModel>>>();

							return unityContainer.Resolve<ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>(
								new ParameterOverride("testSelector", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testProcessor", testProcessorFactory(processingType)));
						})));
		}

		#endregion

		#region TestsDiscoverer

		private static void InitTestsDiscoverer(IUnityContainer unityContainer)
		{
			InitTestsDiscovererForModel<GitCSharpProgramModel>(unityContainer);
			InitTestsDiscovererForModel<TFS2010ProgramModel>(unityContainer);
			InitTestsDiscovererForModel<LocalProgramModel>(unityContainer);
		}

		private static void InitTestsDiscovererForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			//unityContainer.RegisterType<ITestDiscoverer<TModel, MSTestTestcase>, MonoMSTestTestDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestDiscoverer<TModel, MSTestTestcase>, InProcessMSTestTestDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestDiscoverer<TModel, CsvFileTestcase>, CsvTestFileDiscoverer<TModel>>();
		}

		#endregion

		#region DeltaDiscoverer
		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<GitCSharpProgramModel, StructuralDelta<GitCSharpProgramModel, FileElement>>, GitFileDeltaDiscoverer>();
		}

		#endregion

		#region TestsSelectors

		private static void InitTestsSelectors(IUnityContainer unityContainer)
		{
			InitTestSelectorsForCSharpModel<GitCSharpProgramModel>(unityContainer);
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
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase>, ClassSRTSDeltaExpander<TModel, TTestCase>>(RTSApproachType.ClassSRTS.ToString());

			InitTestSelectorsForModelAndElementType<TModel, CSharpFileElement, TTestCase>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpClassElement, TTestCase>(unityContainer);
		}

		private static void InitTestSelectorsForModelAndElementType<TModel, TModelElement, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TModelElement : IProgramModelElement
			where TTestCase : class, ITestCase
		{
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, DynamicRTS<TModel, TModelElement, TTestCase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, RetestAllSelector<TModel, TModelElement, TTestCase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name => c.Resolve<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name.ToString()))));
		}

		#endregion

		#region TestsProcessors

		private static void InitTestsProcessors(IUnityContainer unityContainer)
		{
			InitTestsProcessors<GitCSharpProgramModel>(unityContainer);
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
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, TestExecutorWithInstrumenting<TModel, TDelta, MSTestTestcase>>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());

			//unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, InProcessMSTestTestExecutor<TDelta, TModel>>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestExecutor<MSTestTestcase, TDelta, TModel>, InProcessMSTestTestExecutor<TDelta, TModel>>();

			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, LimitedTimeTestExecutor<MSTestTestcase, TDelta, TModel>>(ProcessingType.MSTestExecutionLimitedTime.ToString());

			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<ITestProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

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
			unityContainer.RegisterType<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TDelta, TModel>>>(
				new InjectionFactory(c =>
				new Func<ProcessingType, ITestProcessor<TTestCase, TResult, TDelta, TModel>>(name => c.Resolve<ITestProcessor<TTestCase, TResult, TDelta, TModel>>(name.ToString()))));
		}
		#endregion

		#region TestPrioritizer

		private static void InitTestsPrioritizers(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestPrioritizer<MSTestTestcase>, NoOpPrioritizer<MSTestTestcase>>();
			unityContainer.RegisterType<ITestPrioritizer<CsvFileTestcase>, NoOpPrioritizer<CsvFileTestcase>>();
		}

		#endregion

		#region TestInstrumentor

		private static void InitTestsInstrumentors(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestsInstrumentor<GitCSharpProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<GitCSharpProgramModel>>();
			unityContainer.RegisterType<ITestsInstrumentor<TFS2010ProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<TFS2010ProgramModel>>();
			unityContainer.RegisterType<ITestsInstrumentor<LocalProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<LocalProgramModel>>();
		}

		#endregion

		#region DataStructureProvider

		private static void InitDataStructureProvider(IUnityContainer unityContainer)
		{
			InitDataStructureProviderForModel<GitCSharpProgramModel>(unityContainer);
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
			unityContainer.RegisterType<IArtefactAdapter<GitVersionIdentification, GitCSharpProgramModel>, GitCSharpProgramModelAdapter>();
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
			unityContainer.RegisterType<IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<LocalProgramModel, FileElement>>, IntendedChangesAdapter>();

			//Cancelable Adapter
			unityContainer.RegisterType<CancelableArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();

			//Delta Adapters
			InitDeltaAdaptersForModels<GitCSharpProgramModel>(unityContainer);
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