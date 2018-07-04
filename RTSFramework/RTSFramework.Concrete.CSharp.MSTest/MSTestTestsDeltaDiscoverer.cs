using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;
using RTSFramework.RTSApproaches.CorrespondenceModel;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestsDeltaDiscoverer<TModel, TDelta> : ITestDiscoverer<TModel, TDelta, MSTestTestcase>
		where TModel : CSharpProgramModel where TDelta : IDelta<TModel>
	{
		private const string TestsModelsStoragePlace = "TestsModels";
		private const string TestTypeIdentifier = "MSTest";

		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly InProcessVsTestConnector vsTestConnector;
		private readonly ISettingsProvider settingsProvider;
		private readonly IUserRunConfigurationProvider runConfiguration;
		private readonly IArtefactAdapter<FileInfo, TestsModel<MSTestTestcase>> testsModelAdapter;

		public MSTestTestsDeltaDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter, 
			InProcessVsTestConnector vsTestConnector,
			ISettingsProvider settingsProvider,
			IUserRunConfigurationProvider runConfiguration,
			IArtefactAdapter<FileInfo, TestsModel<MSTestTestcase>> testsModelAdapter)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
			this.runConfiguration = runConfiguration;
			this.testsModelAdapter = testsModelAdapter;
		}

		public async Task<StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>> GetTests(TDelta delta, Func<MSTestTestcase, bool> filterFunction, CancellationToken token)
		{
			var oldTestsModel = testsModelAdapter.Parse(GetTestsStorage(delta.OldModel.VersionId));

			if (!runConfiguration.DiscoverNewTests && oldTestsModel != null)
			{
				return new StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>(oldTestsModel, oldTestsModel);
			}

			if (oldTestsModel == null)
			{
				oldTestsModel = new TestsModel<MSTestTestcase>
				{
					TestSuite = new HashSet<MSTestTestcase>(),
					VersionId = delta.OldModel.VersionId
				};
			}

			var parsingResult = await assembliesAdapter.Parse(delta.NewModel.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith(settingsProvider.TestAssembliesFilter));

			var vsTestCases = await DiscoverTests(sources, token);

			var newTestsModel = new TestsModel<MSTestTestcase>
			{
				TestSuite = new HashSet<MSTestTestcase>(vsTestCases.Select(Convert).Where(x => !x.Ignored && filterFunction(x))),
				VersionId = delta.NewModel.VersionId
			};
			testsModelAdapter.Unparse(newTestsModel, GetTestsStorage(delta.NewModel.VersionId));

			var testsDelta = new StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>(oldTestsModel, newTestsModel);
			testsDelta.AddedElements.AddRange(newTestsModel.TestSuite.Except(oldTestsModel.TestSuite));
			testsDelta.DeletedElements.AddRange(oldTestsModel.TestSuite.Except(newTestsModel.TestSuite));

			return testsDelta;
		}

		//TODO: Artefact adapter
		private MSTestTestcase Convert(TestCase vsTestCase)
		{
			var testNameProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestClassName);
			var isEnabledProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyIsEnabled);
			var testCategoryProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestCategory);
			var dataDrivenProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyIsDataDriven);

			string testClassName = testNameProperty != null ? vsTestCase.GetPropertyValue(testNameProperty, "") : "";
			bool isEnabled = isEnabledProperty == null || vsTestCase.GetPropertyValue(isEnabledProperty, true);
			string[] categories = testCategoryProperty != null ? vsTestCase.GetPropertyValue(testCategoryProperty, new string[0]) : new string[0];
			bool isDataDriven = dataDrivenProperty != null && vsTestCase.GetPropertyValue(dataDrivenProperty, false);

			var msTestCase = new MSTestTestcase
			{
				Name = vsTestCase.DisplayName,
				AssemblyPath = vsTestCase.Source,
				Id = vsTestCase.FullyQualifiedName,
				AssociatedClass = testClassName,
				Ignored = !isEnabled,
				VsTestTestCase = vsTestCase,
				IsChildTestCase = isDataDriven
			};
			msTestCase.Categories.AddRange(categories);

			return msTestCase;
		}

		private async Task<IEnumerable<TestCase>> DiscoverTests(IEnumerable<string> sources, CancellationToken token)
		{
			var waitHandle = new AsyncAutoResetEvent();
			var handler = new DiscoveryEventHandler(waitHandle);
			var registration = token.Register(vsTestConnector.ConsoleWrapper.CancelDiscovery);

			vsTestConnector.ConsoleWrapper.DiscoverTests(sources, string.Format(MSTestConstants.DefaultRunSettings, Directory.GetCurrentDirectory()), handler);

			await waitHandle.WaitAsync(token);
			registration.Dispose();
			token.ThrowIfCancellationRequested();

			return handler.DiscoveredTestCases;
		}

		//TODO: To Super class
		private FileInfo GetTestsStorage(string programVersionId)
		{
			return new FileInfo(Path.GetFullPath(Path.Combine(TestsModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{TestTypeIdentifier}_{JsonTestsModelAdapter<MSTestTestcase>.FileExtension}")));
		}
	}
}
