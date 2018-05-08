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
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestsDiscoverer<TModel> : ITestsDiscoverer<TModel, MSTestTestcase>
		where TModel : CSharpProgramModel
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly InProcessVsTestConnector vsTestConnector;
		private readonly ISettingsProvider settingsProvider;

		public InProcessMSTestTestsDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter, 
			InProcessVsTestConnector vsTestConnector,
			ISettingsProvider settingsProvider)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
		}

		public async Task<IList<MSTestTestcase>> GetTestCasesForModel(TModel model, CancellationToken token)
		{
			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith(settingsProvider.TestAssembliesFilter));

			var vsTestCases = await DiscoverTests(sources, token);

			return vsTestCases.Select(Convert).ToList();
		}

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
				FullClassName = testClassName,
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
	}
}
