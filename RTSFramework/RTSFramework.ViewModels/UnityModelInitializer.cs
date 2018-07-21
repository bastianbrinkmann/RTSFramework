using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Msagl.Drawing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.Core;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest;
using RTSFramework.Concrete.CSharp.MSTest.Adapters;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Concrete.CSharp.Roslyn;
using RTSFramework.Concrete.CSharp.Roslyn.Adapters;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Concrete.CSharp.Roslyn.ResponsibleChanges;
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010;
using RTSFramework.Concrete.User;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Models.TestExecution;
using RTSFramework.Contracts.SecondaryFeature;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core;
using RTSFramework.Core.DependenciesVisualization;
using RTSFramework.Core.Models;
using RTSFramework.Core.StatisticsReporting;
using RTSFramework.Core.Utilities;
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
			InitTestsDeltaAdapter(unityContainer);
			InitTestsSelectors(unityContainer);
			InitTestsProcessors(unityContainer);
			InitTestsInstrumentors(unityContainer);
			InitTestsPrioritizers(unityContainer);

			InitModelLevelController(unityContainer);
			InitOfflineController(unityContainer);
			InitDeltaBasedController(unityContainer);

			//Secondary Scenarios
			InitDependenciesVisualizer(unityContainer);
			InitStatisticsReporter(unityContainer);
			InitResponsibleChangesReporter(unityContainer);

			container = unityContainer;
		}

		private static IUnityContainer container;
		internal static OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact> 
			GetOfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType, bool withTimeLimit)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, bool, OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>>>();

			return factory(rtsApproachType, processingType, withTimeLimit);
		}

		internal static DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact> 
			GetDeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType, bool withTimeLimit)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>>>();

			return factory(rtsApproachType, processingType, withTimeLimit);
		}

		private static void InitHelper(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IUserRunConfigurationProvider, UserRunConfigurationProvider>(new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<InProcessVsTestConnector>(new ContainerControlledLifetimeManager());
		}

		#region DeltaBasedController

		private static void InitDeltaBasedController(IUnityContainer unityContainer)
		{
			InitDeltaBasedControllerFactoryDelta<IntendedChangesArtefact, FilesProgramModel>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryDelta<TDeltaArtefact, TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, StructuralDelta<TModel, FileElement>>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryTestcase<TDeltaArtefact, TModel, TProgramDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TProgramDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TProgramDelta, MSTestTestcase>(unityContainer);
			InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TProgramDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactoryResult<TDeltaArtefact, TModel, TProgramDelta, TTestCase>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TProgramDelta, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>(
						(rtsApproachType, processingType, withTimeLimit) =>
						{
							ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController;
							if (withTimeLimit)
							{
								var limitedTimeControllerFactory =
									unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
										LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>();
								modelBasedController = limitedTimeControllerFactory(rtsApproachType, processingType);
							}
							else
							{
								var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>();
								modelBasedController = modelLevelControllerFactory(rtsApproachType, processingType);
							}

							return unityContainer.Resolve<DeltaBasedController<TDeltaArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>(
								new ParameterOverride("modelBasedController", modelBasedController));

						})));
		}

		#endregion

		#region OfflineController

		private static void InitOfflineController(IUnityContainer unityContainer)
		{
			InitOfflineControllerFactoryDelta<GitVersionIdentification, FilesProgramModel>(unityContainer);
		}

		private static void InitOfflineControllerFactoryDelta<TArtefact, TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitOfflineControllerFactoryTestcase<TArtefact, TModel, StructuralDelta<TModel, FileElement>>(unityContainer);
		}

		private static void InitOfflineControllerFactoryTestcase<TArtefact, TModel, TProgramDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
		{
			InitOfflineControllerFactory<TArtefact, TModel, TProgramDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, object>(unityContainer);

			InitOfflineControllerFactoryResult<TArtefact, TModel, TProgramDelta, MSTestTestcase>(unityContainer);
			InitOfflineControllerFactoryResult<TArtefact, TModel, TProgramDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitOfflineControllerFactoryResult<TArtefact, TModel, TProgramDelta, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitOfflineControllerFactory<TArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, CsvFileArtefact>(unityContainer);
			InitOfflineControllerFactory<TArtefact, TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>, IList<TestResultListViewItemViewModel>>(unityContainer);
			InitOfflineControllerFactory<TArtefact, TModel, TProgramDelta, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(unityContainer);
		}

		private static void InitOfflineControllerFactory<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase 
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, bool, OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, bool, OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>(
						(rtsApproachType, processingType, withTimeLimit) =>
						{
							ModelBasedController<TModel, TProgramDelta, TTestCase, TResult> modelBasedController;
							if (withTimeLimit)
							{
								var limitedTimeControllerFactory =
									unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
										LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>();
								modelBasedController = limitedTimeControllerFactory(rtsApproachType, processingType);
							}
							else
							{
								var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>();
								modelBasedController = modelLevelControllerFactory(rtsApproachType, processingType);
							}
							

							return unityContainer.Resolve<OfflineController<TArtefact, TModel, TProgramDelta, TTestCase, TResult, TResultArtefact, Graph>>(
								new ParameterOverride("modelBasedController", modelBasedController));
						})));
		}

		#endregion

		#region ModelBasedController

		private static void InitModelLevelController(IUnityContainer unityContainer)
		{
			InitModelLevelControllerFactoryDelta<FilesProgramModel>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryDelta<TModel>(IUnityContainer unityContainer)
			where TModel : IProgramModel
		{
			InitModelLevelControllerFactoryTestcase<TModel, StructuralDelta<TModel, FileElement>>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryTestcase<TModel, TProgramDelta>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
		{
			InitModelLevelControllerFactory<TModel, TProgramDelta, MSTestTestcase, ITestsExecutionResult<MSTestTestcase>>(unityContainer);

			InitModelLevelControllerFactoryResult<TModel, TProgramDelta, MSTestTestcase>(unityContainer);
			InitModelLevelControllerFactoryResult<TModel, TProgramDelta, CsvFileTestcase>(unityContainer);
		}

		private static void InitModelLevelControllerFactoryResult<TModel, TProgramDelta, TTestCase>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			InitModelLevelControllerFactory<TModel, TProgramDelta, TTestCase, TestListResult<TTestCase>>(unityContainer);
			InitModelLevelControllerFactory<TModel, TProgramDelta, TTestCase, PercentageImpactedTestsStatistic>(unityContainer);
		}

		private static void InitModelLevelControllerFactory<TModel, TProgramDelta, TTestCase, TResult>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestSelector<TModel, TProgramDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TProgramDelta, TModel>>>();

							return unityContainer.Resolve<ModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>(
								new ParameterOverride("testSelector", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testProcessor", testProcessorFactory(processingType)));
						})));
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestSelector<TModel, TProgramDelta, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TProgramDelta, TModel>>>();

							return unityContainer.Resolve<LimitedTimeModelBasedController<TModel, TProgramDelta, TTestCase, TResult>>(
								new ParameterOverride("testSelector", rtsApproachFactory(rtsApproachType)),
								new ParameterOverride("testProcessor", testProcessorFactory(processingType)));
						})));
		}

		#endregion

		#region TestsDeltaAdapter

		private static void InitTestsDeltaAdapter(IUnityContainer unityContainer)
		{
			InitTestsDeltaAdapterForModel<FilesProgramModel>(unityContainer);
		}

		private static void InitTestsDeltaAdapterForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			InitTestsDeltaAdapterForProgramDelta<TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
			InitTestsDeltaAdapterForProgramDelta<TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitTestsDeltaAdapterForProgramDelta<TModel, StructuralDelta<TModel, FileElement>>(unityContainer);
		}

		private static void InitTestsDeltaAdapterForProgramDelta<TModel, TDelta>(IUnityContainer unityContainer) where TModel : CSharpProgramModel where TDelta: IDelta<TModel>
		{
			unityContainer.RegisterType<ITestsDeltaAdapter<TModel, TDelta, MSTestTestcase>, MSTestTestsDeltaAdapter<TModel, TDelta>>(new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestsDeltaAdapter<TModel, TDelta, CsvFileTestcase>, CsvManualTestsDeltaAdapter<TModel, TDelta>>();
		}

		#endregion

		#region DeltaDiscoverer
		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineDeltaDiscoverer<FilesProgramModel, StructuralDelta<FilesProgramModel, FileElement>>, FileDeltaDiscoverer>();
		}

		#endregion

		#region TestsSelectors

		private static void InitTestsSelectors(IUnityContainer unityContainer)
		{
			InitTestSelectorsForCSharpModel<FilesProgramModel>(unityContainer);
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
			unityContainer.RegisterType<IStaticRTS<CSharpClassesProgramModel, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, TTestCase, IntertypeRelationGraph>, ClassSRTS<TTestCase>>();

			InitTestSelectorsForModelAndElementType<TModel, FileElement, TTestCase>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpFileElement, TTestCase>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpClassElement, TTestCase>(unityContainer);
		}

		private static void InitTestSelectorsForModelAndElementType<TModel, TModelElement, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TModelElement : IProgramModelElement
			where TTestCase : class, ITestCase
		{
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, StaticTestSelector<TModel, CSharpClassesProgramModel, StructuralDelta<TModel, TModelElement>, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, TTestCase, IntertypeRelationGraph>>(RTSApproachType.ClassSRTS.ToString());
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, DynamicTestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, RetestAllSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(RTSApproachType.RetestAll.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name => c.Resolve<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>>(name.ToString()))));
		}

		#endregion

		#region TestsProcessors

		private static void InitTestsProcessors(IUnityContainer unityContainer)
		{
			InitTestsProcessors<FilesProgramModel>(unityContainer);
		}

		private static void InitTestsProcessors<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			InitTestsProcessors<StructuralDelta<TModel, FileElement>, TModel>(unityContainer);
		}

		private static void InitTestsProcessors<TDelta, TModel>(IUnityContainer unityContainer) where TDelta : IDelta<TModel> where TModel : IProgramModel
		{
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, TestExecutorWithInstrumenting<TModel, TDelta, MSTestTestcase>>(ProcessingType.MSTestExecutionCreateCorrespondenceModel.ToString());

			//unityContainer.RegisterType<ITestsProcessor<MSTestTestcase, MSTestExectionResult>, ConsoleMSTestTestsExecutor>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, MSTestTestExecutor<TDelta, TModel>>(ProcessingType.MSTestExecution.ToString());

			unityContainer.RegisterType<ITestExecutor<MSTestTestcase, TDelta, TModel>, MSTestTestExecutor<TDelta, TModel>>(new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, TestListResult<MSTestTestcase>, TDelta, TModel>, TestReporter<MSTestTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<ITestProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.ListReporting.ToString(), new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestProcessor<CsvFileTestcase, TestListResult<CsvFileTestcase>, TDelta, TModel>, TestReporter<CsvFileTestcase, TDelta, TModel>>(ProcessingType.CsvReporting.ToString(), new ContainerControlledLifetimeManager());

			unityContainer.RegisterType<ITestProcessor<CsvFileTestcase, PercentageImpactedTestsStatistic, TDelta, TModel>, PercentageImpactedTestsStatisticsCollector<CsvFileTestcase, TDelta, TModel>>(ProcessingType.CollectStatistics.ToString());
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, PercentageImpactedTestsStatistic, TDelta, TModel>, PercentageImpactedTestsStatisticsCollector<MSTestTestcase, TDelta, TModel>>(ProcessingType.CollectStatistics.ToString());

			InitTestProcessorsFactoryForTestType<ITestsExecutionResult<MSTestTestcase>, TDelta, TModel, MSTestTestcase>(unityContainer);

			InitTestProcessorsFactoryForTestType<TestListResult<MSTestTestcase>, TDelta, TModel, MSTestTestcase>(unityContainer);
			InitTestProcessorsFactoryForTestType<TestListResult<CsvFileTestcase>, TDelta, TModel, CsvFileTestcase>(unityContainer);

			InitTestProcessorsFactoryForTestType<PercentageImpactedTestsStatistic, TDelta, TModel, MSTestTestcase>(unityContainer);
			InitTestProcessorsFactoryForTestType<PercentageImpactedTestsStatistic, TDelta, TModel, CsvFileTestcase>(unityContainer);
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
			unityContainer.RegisterType<ITestsInstrumentor<FilesProgramModel, MSTestTestcase>, MSTestTestsInstrumentor<FilesProgramModel>>();
		}

		#endregion

		#region DataStructureProvider

		private static void InitDataStructureProvider(IUnityContainer unityContainer)
		{
			InitDataStructureProviderForModel<FilesProgramModel>(unityContainer);
			InitDataStructureProviderForModel<CSharpFilesProgramModel>(unityContainer);
			InitDataStructureProviderForModel<CSharpClassesProgramModel>(unityContainer);
		}

		private static void InitDataStructureProviderForModel<TModel>(IUnityContainer unityContainer) where TModel : CSharpProgramModel
		{
			unityContainer.RegisterType<IDataStructureBuilder<IntertypeRelationGraph, TModel>, MonoIntertypeRelationGraphBuilder<TModel>>();
			//unityContainer.RegisterType<IDataStructureBuilder<IntertypeRelationGraph, TModel>, RoslynIntertypeRelationGraphBuilder<TModel>>();
			unityContainer.RegisterType<CorrespondenceModelManager<TModel>>(new ContainerControlledLifetimeManager());
		}

		#endregion

		#region Adapters

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<GitVersionIdentification, FilesProgramModel>, GitFilesProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<TFS2010VersionIdentification, FilesProgramModel>, TFS2010ProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<Graph, VisualizationData>, VisualizationDataMsaglGraphAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<CsvFileArtefact, PercentageImpactedTestsStatistic>, PercentageImpactedTestsStatisticCsvFileAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, StatisticsReportData>, StatisticsReportDataStringAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, TestsModel<MSTestTestcase>>, JsonTestsModelAdapter<MSTestTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, TestsModel<CsvFileTestcase>>, JsonTestsModelAdapter<CsvFileTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<TestCase, MSTestTestcase>, VsTestCaseMSTestTestcaseAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<VsTestResultsToConvert, IList<ITestCaseResult<MSTestTestcase>>>, VsTestResultsAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<VsTestResultToConvert, ITestCaseResult<MSTestTestcase>>, VsTestResultAdapter>();
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
			unityContainer.RegisterType<IArtefactAdapter<IntendedChangesArtefact, StructuralDelta<FilesProgramModel, FileElement>>, IntendedChangesAdapter>();

			//Cancelable Adapter
			unityContainer.RegisterType<CancelableArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();

			//Delta Adapters
			InitDeltaAdaptersForModels<FilesProgramModel>(unityContainer);
		}

		private static void InitDeltaAdaptersForModels<TModel>(IUnityContainer unityContainer) where TModel : IProgramModel
		{
			InitDeltaAdaptersForModelElement<TModel, FileElement>(unityContainer);
			InitDeltaAdaptersForModelElement<TModel, CSharpFileElement>(unityContainer);
			InitDeltaAdaptersForModelElement<TModel, CSharpClassElement>(unityContainer);

			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, CSharpFilesProgramModel, CSharpClassesProgramModel>, CSharpFileClassDeltaAdapter>();
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<FilesProgramModel, FileElement>, StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>, FilesProgramModel, CSharpFilesProgramModel>, FilesCSharpFilesDeltaAdapter>();
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<FilesProgramModel, FileElement>, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, FilesProgramModel, CSharpClassesProgramModel>,
				ChainingDeltaAdapter<StructuralDelta<FilesProgramModel, FileElement>, StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, FilesProgramModel, CSharpFilesProgramModel, CSharpClassesProgramModel>>();
		}

		private static void InitDeltaAdaptersForModelElement<TModel, TModelElement>(IUnityContainer unityContainer) where TModel : IProgramModel
			where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<IDeltaAdapter<StructuralDelta<TModel, TModelElement>, StructuralDelta<TModel, TModelElement>, TModel, TModel>, IdentityDeltaAdapter<StructuralDelta<TModel, TModelElement>, TModel>>();
		}

		#endregion

		#region SecondaryFeatures

		private static void InitDependenciesVisualizer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IDependenciesVisualizer, Random25LinksVisualizer>();
		}

		private static void InitStatisticsReporter(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IStatisticsReporter, AveragePercentageImpactedTestsReporter>();
		}

		private static void InitResponsibleChangesReporter(IUnityContainer unityContainer)
		{
			InitResponsibleChangesReporterForTestTypes<MSTestTestcase>(unityContainer);
			InitResponsibleChangesReporterForTestTypes<CsvFileTestcase>(unityContainer);
		}

		private static void InitResponsibleChangesReporterForTestTypes<TTestCase>(IUnityContainer unityContainer)
			where TTestCase : ITestCase
		{
			InitResponsibleChangesReporterForModel<TTestCase, FilesProgramModel>(unityContainer);
		}

		private static void InitResponsibleChangesReporterForModel<TTestCase, TModel>(IUnityContainer unityContainer)
			where TTestCase : ITestCase
			where TModel : IProgramModel
		{
			InitResponsibleChangesReporterForDelta<TTestCase, TModel, FileElement>(unityContainer);
			InitResponsibleChangesReporterForDelta<TTestCase, TModel, CSharpFileElement>(unityContainer);
			InitResponsibleChangesReporterForDelta<TTestCase, TModel, CSharpClassElement>(unityContainer);
		}

		private static void InitResponsibleChangesReporterForDelta<TTestCase, TModel, TModelElement>(IUnityContainer unityContainer)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TModelElement : IProgramModelElement
		{
			unityContainer.RegisterType<IResponsibleChangesReporter<TTestCase, TModel, StructuralDelta<TModel, TModelElement>>, 
				ClassLevelResponsibleChangesReporter<TTestCase, TModel, TModelElement>>();
		}

		#endregion

	}
}