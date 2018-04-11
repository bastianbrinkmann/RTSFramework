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
using RTSFramework.Concrete.Git;
using RTSFramework.Concrete.Git.Models;
using RTSFramework.Concrete.Reporting;
using RTSFramework.Concrete.TFS2010.Models;
using RTSFramework.Concrete.User;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.RTSApproach;
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

			InitDeltaDiscoverer(unityContainer);
			InitTestsDiscoverer(unityContainer);
			InitRTSApproaches(unityContainer);
			InitTestsProcessors(unityContainer);
		}

		private static void InitHelper(IUnityContainer unityContainer)
		{
			//FilesProvider
			unityContainer.RegisterType<IFilesProvider, GitFilesProvider>(typeof(GitProgramModel).FullName);
			//TODO Replace by TFS 2010 FilesProvider
			unityContainer.RegisterType<IFilesProvider, LocalFilesProvider>(typeof(TFS2010ProgramModel).FullName);
			
			//FilesProviderFactory
			unityContainer.RegisterType<Func<IProgramModel, IFilesProvider>>(
				new InjectionFactory(c =>
				new Func<IProgramModel, IFilesProvider>(model => c.Resolve<IFilesProvider>(model.GetType().FullName))));

			unityContainer.RegisterType<IntertypeRelationGraphBuilder>();
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
			unityContainer.RegisterType<IRTSApproach<MSTestTestcase>, DynamicRTSApproach<MSTestTestcase>>(RTSApproachType.DynamicRTS.ToString());
			unityContainer.RegisterType<IRTSApproach<MSTestTestcase>, RetestAllApproach<MSTestTestcase>>(RTSApproachType.RetestAll.ToString());
			unityContainer.RegisterType<IRTSApproach<MSTestTestcase>, ClassSRTSApproach>(RTSApproachType.ClassSRTS.ToString());

			unityContainer.RegisterType<Func<RTSApproachType, IRTSApproach<MSTestTestcase>>>(
				new InjectionFactory(c =>
				new Func<RTSApproachType, IRTSApproach<MSTestTestcase>>(name => c.Resolve<IRTSApproach<MSTestTestcase>>(name.ToString()))));
		}

		private static void InitTestsDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<ITestsDiscoverer<MSTestTestcase>, MSTestTestsDiscoverer>();
		}

		private static void InitDeltaDiscoverer(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<IOfflineFileDeltaDiscoverer, LocalGitFileDeltaDiscoverer>(DiscoveryType.LocalDiscovery.ToString());
			unityContainer.RegisterType<IOfflineFileDeltaDiscoverer, UserIntendedChangesDiscoverer>(DiscoveryType.UserIntendedChangesDiscovery.ToString());

			//NestedDiscoverers
			unityContainer.RegisterType<IOfflineDeltaDiscoverer, CSharpFilesDeltaDiscoverer>(GranularityLevel.File.ToString());
			unityContainer.RegisterType<IOfflineDeltaDiscoverer, CSharpClassDeltaDiscoverer>(GranularityLevel.Class.ToString());

			InitDiscovererFactories(unityContainer);
		}

		private static void InitDiscovererFactories(IUnityContainer unityContainer)
		{
			unityContainer.RegisterType<Func<DiscoveryType, IOfflineFileDeltaDiscoverer>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, IOfflineFileDeltaDiscoverer>(name => c.Resolve<IOfflineFileDeltaDiscoverer>(name.ToString()))));

			unityContainer.RegisterType<Func<DiscoveryType, GranularityLevel, IOfflineDeltaDiscoverer>>(
				new InjectionFactory(c =>
				new Func<DiscoveryType, GranularityLevel, IOfflineDeltaDiscoverer> ((discoveryType, granularityLevel) =>
				{
					var fileDeltaDiscovererFactory = c.Resolve<Func<DiscoveryType, IOfflineFileDeltaDiscoverer>>();
					var internalFileDeltaDiscoverer = fileDeltaDiscovererFactory(discoveryType);

					var fileDeltaDiscoverer = c.Resolve<IOfflineDeltaDiscoverer>(GranularityLevel.File.ToString(), new ParameterOverride("internalDiscoverer", internalFileDeltaDiscoverer));

					if (granularityLevel == GranularityLevel.File)
					{
						return fileDeltaDiscoverer;
					}

					return c.Resolve<IOfflineDeltaDiscoverer>(granularityLevel.ToString(), new ParameterOverride("internalDiscoverer", fileDeltaDiscoverer));
				})));
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