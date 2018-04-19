using System.IO;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

namespace RTSFramework.Concrete.CSharp.MSTest.VsTest
{
	public class InProcessVsTestConnector
	{
		private IVsTestConsoleWrapper consoleWrapper;

		public IVsTestConsoleWrapper ConsoleWrapper
		{
			get
			{
				if (consoleWrapper == null)
				{
					var vsTestConsole = Path.GetFullPath(Path.Combine(MSTestConstants.VstestPath, MSTestConstants.Vstestconsole));
					consoleWrapper = new VsTestConsoleWrapper(vsTestConsole);
					consoleWrapper.StartSession();
				}

				return consoleWrapper;
			}
		}
	}
}