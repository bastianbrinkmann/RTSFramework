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
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestDiscoverer<TModel, TDelta> : ITestDiscoverer<TModel, TDelta, MSTestTestcase>
		where TModel : CSharpProgramModel where TDelta : IDelta<TModel>
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly InProcessVsTestConnector vsTestConnector;
		private readonly ISettingsProvider settingsProvider;
		private readonly IUserRunConfigurationProvider runConfiguration;

		public InProcessMSTestTestDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter, 
			InProcessVsTestConnector vsTestConnector,
			ISettingsProvider settingsProvider,
			IUserRunConfigurationProvider runConfiguration)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.vsTestConnector = vsTestConnector;
			this.settingsProvider = settingsProvider;
			this.runConfiguration = runConfiguration;
		}

		private ISet<MSTestTestcase> discoveredTests;

		public async Task<ISet<MSTestTestcase>> GetTests(TDelta delta, Func<MSTestTestcase, bool> filterFunction, CancellationToken token)
		{
			var model = delta.NewModel;

			if (!runConfiguration.DiscoverNewTests && discoveredTests != null)
			{
				return discoveredTests;
			}

			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith(settingsProvider.TestAssembliesFilter));

			var vsTestCases = await DiscoverTests(sources, token);

			discoveredTests = new HashSet<MSTestTestcase>(vsTestCases.Select(Convert).Where(x => !x.Ignored && filterFunction(x)));

			return discoveredTests;
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
	}
}
