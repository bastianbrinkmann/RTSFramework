﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Msagl.Drawing;
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
			InitTestsDiscoverer(unityContainer);
			InitTestsSelectors(unityContainer);
			InitTestsProcessors(unityContainer);
			InitTestsInstrumentors(unityContainer);
			InitTestsPrioritizers(unityContainer);

			InitModelLevelController(unityContainer);
			InitStateBasedController(unityContainer);
			InitDeltaBasedController(unityContainer);

			//Secondary Scenarios
			InitDependenciesVisualizer(unityContainer);
			InitStatisticsReporter(unityContainer);

			container = unityContainer;
		}

		private static IUnityContainer container;
		internal static StateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact> 
			GetStateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType, bool withTimeLimit)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TDiscoveryDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, bool, StateBasedController<TArtefact, TModel, TDiscoveryDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>>>();

			return factory(rtsApproachType, processingType, withTimeLimit);
		}

		internal static DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact> 
			GetDeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>
			(RTSApproachType rtsApproachType, ProcessingType processingType, bool withTimeLimit)
			where TTestCase : ITestCase
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TResult : ITestProcessingResult
		{
			var factory = container.Resolve<Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, TVisualizationArtefact>>>();

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
			InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(unityContainer);
		}

		private static void InitDeltaBasedControllerFactory<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer)
			where TModel : IProgramModel
			where TParsedDelta : IDelta<TModel>
			where TSelectionDelta : IDelta<TModel>
			where TTestCase : ITestCase
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, Graph>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, bool, DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, Graph>>(
						(rtsApproachType, processingType, withTimeLimit) =>
						{
							ModelBasedController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult> modelBasedController;
							if (withTimeLimit)
							{
								var limitedTimeControllerFactory =
									unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
										LimitedTimeModelBasedController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult>>>();
								modelBasedController = limitedTimeControllerFactory(rtsApproachType, processingType);
							}
							else
							{
								var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult>>>();
								modelBasedController = modelLevelControllerFactory(rtsApproachType, processingType);
							}

							return unityContainer.Resolve<DeltaBasedController<TDeltaArtefact, TModel, TParsedDelta, TSelectionDelta, TTestCase, TResult, TResultArtefact, Graph>>(
								new ParameterOverride("modelBasedController", modelBasedController));

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
			InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, PercentageImpactedTestsStatistic, CsvFileArtefact>(unityContainer);
		}

		private static void InitStateBasedControllerFactory<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact>(IUnityContainer unityContainer) 
			where TModel : IProgramModel 
			where TDeltaDisovery : IDelta<TModel>
			where TDeltaSelection : IDelta<TModel>
			where TTestCase : ITestCase 
			where TResult : ITestProcessingResult
		{
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, bool, StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact, Graph>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, bool, StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact, Graph>>(
						(rtsApproachType, processingType, withTimeLimit) =>
						{
							ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult> modelBasedController;
							if (withTimeLimit)
							{
								var limitedTimeControllerFactory =
									unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
										LimitedTimeModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>>();
								modelBasedController = limitedTimeControllerFactory(rtsApproachType, processingType);
							}
							else
							{
								var modelLevelControllerFactory =
								unityContainer.Resolve<Func<RTSApproachType, ProcessingType,
											ModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>>();
								modelBasedController = modelLevelControllerFactory(rtsApproachType, processingType);
							}
							

							return unityContainer.Resolve<StateBasedController<TArtefact, TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult, TResultArtefact, Graph>>(
								new ParameterOverride("modelBasedController", modelBasedController));
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
			InitModelLevelControllerFactory<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, PercentageImpactedTestsStatistic>(unityContainer);
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
			unityContainer.RegisterType<Func<RTSApproachType, ProcessingType, LimitedTimeModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>>(
				new InjectionFactory(c =>
					new Func<RTSApproachType, ProcessingType, LimitedTimeModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>(
						(rtsApproachType, processingType) =>
						{
							var rtsApproachFactory = unityContainer.Resolve<Func<RTSApproachType, ITestSelector<TModel, TDeltaSelection, TTestCase>>>();
							var testProcessorFactory = unityContainer.Resolve<Func<ProcessingType, ITestProcessor<TTestCase, TResult, TDeltaSelection, TModel>>>();

							return unityContainer.Resolve<LimitedTimeModelBasedController<TModel, TDeltaDisovery, TDeltaSelection, TTestCase, TResult>>(
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
			InitTestsDiscovererForDelta<TModel, StructuralDelta<TModel, CSharpClassElement>>(unityContainer);
			InitTestsDiscovererForDelta<TModel, StructuralDelta<TModel, CSharpFileElement>>(unityContainer);
			InitTestsDiscovererForDelta<TModel, StructuralDelta<TModel, FileElement>>(unityContainer);
		}

		private static void InitTestsDiscovererForDelta<TModel, TDelta>(IUnityContainer unityContainer) where TModel : CSharpProgramModel where TDelta: IDelta<TModel>
		{
			//unityContainer.RegisterType<ITestDiscoverer<TModel, MSTestTestcase>, MonoMSTestTestDiscoverer<TModel>>();
			unityContainer.RegisterType<ITestDiscoverer<TModel, TDelta, MSTestTestcase>, MSTestTestsDeltaDiscoverer<TModel, TDelta>>(new ContainerControlledLifetimeManager());
			unityContainer.RegisterType<ITestDiscoverer<TModel, TDelta, CsvFileTestcase>, CsvManualTestsDeltaDiscoverer<TModel, TDelta>>();
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
			unityContainer
				.RegisterType<IStaticRTS<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase, IntertypeRelationGraph>, ClassSRTS<TModel, TTestCase>>();
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase>, StaticTestSelector<TModel, StructuralDelta<TModel, CSharpClassElement>, TTestCase, IntertypeRelationGraph>>(RTSApproachType.ClassSRTS.ToString());

			InitTestSelectorsForModelAndElementType<TModel, CSharpFileElement, TTestCase>(unityContainer);
			InitTestSelectorsForModelAndElementType<TModel, CSharpClassElement, TTestCase>(unityContainer);
		}

		private static void InitTestSelectorsForModelAndElementType<TModel, TModelElement, TTestCase>(IUnityContainer unityContainer) 
			where TModel : IProgramModel
			where TModelElement : IProgramModelElement
			where TTestCase : class, ITestCase
		{
			unityContainer.RegisterType<ITestSelector<TModel, StructuralDelta<TModel, TModelElement>, TTestCase>, DynamicTestSelector<TModel, TModelElement, TTestCase>>(RTSApproachType.DynamicRTS.ToString());
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
			unityContainer.RegisterType<ITestProcessor<MSTestTestcase, ITestsExecutionResult<MSTestTestcase>, TDelta, TModel>, MSTestTestExecutor<TDelta, TModel>>(ProcessingType.MSTestExecution.ToString());
			unityContainer.RegisterType<ITestExecutor<MSTestTestcase, TDelta, TModel>, MSTestTestExecutor<TDelta, TModel>>();

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
			//unityContainer.RegisterType<IDataStructureProvider<IntertypeRelationGraph, TModel>, RoslynIntertypeRelationGraphBuilder<TModel>>();
			unityContainer.RegisterType<CorrespondenceModelManager<TModel>>(new ContainerControlledLifetimeManager());
		}

		#endregion

		#region Adapters

		private static void InitAdapters(IUnityContainer unityContainer)
		{
			//Artefact Adapters
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, CorrespondenceModel>, JsonCorrespondenceModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>, TrxFileMsTestExecutionResultAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<MSTestExecutionResultParameters, CorrespondenceLinks>, OpenCoverXmlCoverageAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, IList<CSharpAssembly>>, SolutionAssembliesAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<GitVersionIdentification, GitCSharpProgramModel>, GitCSharpProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<TFS2010VersionIdentification, TFS2010ProgramModel>, TFS2010ProgramModelAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<Graph, VisualizationData>, VisualizationDataMsaglGraphAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<CsvFileArtefact, PercentageImpactedTestsStatistic>, PercentageImpactedTestsStatisticCsvFileAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<string, StatisticsReportData>, StatisticsReportDataStringAdapter>();
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, ISet<MSTestTestcase>>, JsonTestsModelAdapter<MSTestTestcase>>();
			unityContainer.RegisterType<IArtefactAdapter<FileInfo, ISet<CsvFileTestcase>>, JsonTestsModelAdapter<CsvFileTestcase>>();

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

		private static void InitDependenciesVisualizer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IDependenciesVisualizer, Random25LinksVisualizer>();
		}

		private static void InitStatisticsReporter(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IStatisticsReporter, AveragePercentageImpactedTestsReporter>();
		}
	}
}