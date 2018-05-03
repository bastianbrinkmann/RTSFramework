using System;
using System.Configuration;
using RTSFramework.Contracts;

namespace RTSFramework.GUI
{
	public class SettingsProvider : ISettingsProvider
	{
		public SettingsProvider()
		{
			Configuration = ConfigurationManager.AppSettings.Get("configuration");
			Platform = ConfigurationManager.AppSettings.Get("platform");
			TestAssembliesFilter = ConfigurationManager.AppSettings.Get("testAssembliesFilter");

			bool cleanup;
			if (bool.TryParse(ConfigurationManager.AppSettings.Get("cleanupTestResultsDirectory"), out cleanup))
			{
				CleanupTestResultsDirectory = cleanup;
			}

			bool log;
			if (bool.TryParse(ConfigurationManager.AppSettings.Get("logToFile"), out log))
			{
				LogToFile = log;
			}
		}

		public string Configuration { get; }
		public string Platform { get; }
		public string TestAssembliesFilter { get; }
		public bool CleanupTestResultsDirectory { get; }
		public bool LogToFile { get; }
	}
}