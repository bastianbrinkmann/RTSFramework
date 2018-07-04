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
		private readonly IArtefactAdapter<TestCase, MSTestTestcase> vsTestCaseAdapter;

		public MSTestTestsDeltaDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter, 
			InProcessVsTestConnector vsTestConnector,
			ISettingsProvider settingsProvider,
			IUserRunConfigurationProvider runConfiguration,
			IArtefactAdapter<FileInfo, TestsModel<MSTestTestcase>> testsModelAdapter,
			IArtefactAdapter<TestCase, MSTestTestcase> vsTestCaseAdapter)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
			this.runConfiguration = runConfiguration;
			this.testsModelAdapter = testsModelAdapter;
			this.vsTestCaseAdapter = vsTestCaseAdapter;
		}

		public async Task<StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>> GetTestsDelta(TDelta programDelta, Func<MSTestTestcase, bool> filterFunction, CancellationToken token)
		{
			var oldTestsModel = testsModelAdapter.Parse(GetTestsStorage(programDelta.OldModel.VersionId));

			if (!runConfiguration.DiscoverNewTests && oldTestsModel != null)
			{
				return new StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>(oldTestsModel, oldTestsModel);
			}

			if (oldTestsModel == null)
			{
				oldTestsModel = new TestsModel<MSTestTestcase>
				{
					TestSuite = new HashSet<MSTestTestcase>(),
					VersionId = programDelta.OldModel.VersionId
				};
			}

			var parsingResult = await assembliesAdapter.Parse(programDelta.NewModel.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith(settingsProvider.TestAssembliesFilter));

			var vsTestCases = await DiscoverTests(sources, token);

			var newTestsModel = new TestsModel<MSTestTestcase>
			{
				TestSuite = new HashSet<MSTestTestcase>(vsTestCases.Select(x => vsTestCaseAdapter.Parse(x)).Where(x => !x.Ignored && filterFunction(x))),
				VersionId = programDelta.NewModel.VersionId
			};
			testsModelAdapter.Unparse(newTestsModel, GetTestsStorage(programDelta.NewModel.VersionId));

			var testsDelta = new StructuralDelta<TestsModel<MSTestTestcase>, MSTestTestcase>(oldTestsModel, newTestsModel);
			testsDelta.AddedElements.AddRange(newTestsModel.TestSuite.Except(oldTestsModel.TestSuite));
			testsDelta.DeletedElements.AddRange(oldTestsModel.TestSuite.Except(newTestsModel.TestSuite));

			return testsDelta;
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
