using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.VsTest;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class InProcessMSTestTestsDiscoverer<TModel> : ITestsDiscoverer<TModel, MSTestTestcase>
		where TModel : CSharpProgramModel
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;

		public InProcessMSTestTestsDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter)
		{
			this.assembliesAdapter = assembliesAdapter;
		}

		private const string DefaultRunSettings = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

		public async Task<IEnumerable<MSTestTestcase>> GetTestCasesForModel(TModel model, CancellationToken token)
		{
			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith("Test.dll"));
			var vsTestConsole = Path.GetFullPath(Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole));

			IVsTestConsoleWrapper consoleWrapper = new VsTestConsoleWrapper(vsTestConsole);

			consoleWrapper.StartSession();

			var vsTestCases = await DiscoverTests(sources, consoleWrapper, token);

			return vsTestCases.Select(Convert);
		}

		private MSTestTestcase Convert(TestCase vsTestCase)
		{
			var testNameProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestClassName);
			var isEnabledProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyIsEnabled);
			var testCategoryProperty = vsTestCase.Properties.SingleOrDefault(x => x.Id == MSTestConstants.PropertyTestCategory);

			string testClassName = testNameProperty != null ? vsTestCase.GetPropertyValue(testNameProperty, "") : "";
			bool isEnabled = isEnabledProperty == null || vsTestCase.GetPropertyValue(isEnabledProperty, true);
			string[] categories = testCategoryProperty != null ? vsTestCase.GetPropertyValue(testCategoryProperty, new string[0]) : new string[0];

			var msTestCase = new MSTestTestcase
			{
				Name = vsTestCase.DisplayName,
				AssemblyPath = vsTestCase.Source,
				Id = vsTestCase.FullyQualifiedName,
				FullClassName = testClassName,
				Ignored = !isEnabled
			};
			msTestCase.Categories.AddRange(categories);

			return msTestCase;
		}

		private async Task<IEnumerable<TestCase>> DiscoverTests(IEnumerable<string> sources, IVsTestConsoleWrapper consoleWrapper, CancellationToken token)
		{
			var waitHandle = new AsyncAutoResetEvent();
			var handler = new DiscoveryEventHandler(waitHandle);

			consoleWrapper.DiscoverTests(sources, DefaultRunSettings, handler);
			token.Register(consoleWrapper.CancelDiscovery);
			
			await waitHandle.WaitAsync();
			token.ThrowIfCancellationRequested();

			return handler.DiscoveredTestCases;
		}
	}
}
