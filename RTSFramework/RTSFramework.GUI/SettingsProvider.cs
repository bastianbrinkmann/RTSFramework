using System;
using System.Configuration;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Utilities;

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

			AdditionalReferences = ConfigurationManager.AppSettings.Get("additionalReferences").Split(';');

			double fontSize;
			if (double.TryParse(ConfigurationManager.AppSettings.Get("fontSize"), out fontSize))
			{
				FontSize = fontSize;
			}
			else
			{
				FontSize = 12;
			}
		}

		public string Configuration { get; }
		public string Platform { get; }
		public string TestAssembliesFilter { get; }
		public bool CleanupTestResultsDirectory { get; }
		public bool LogToFile { get; }
		public string[] AdditionalReferences { get; }
		public double FontSize { get; }
	}
}