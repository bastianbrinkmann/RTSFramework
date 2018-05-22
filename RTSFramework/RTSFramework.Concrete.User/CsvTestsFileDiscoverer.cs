using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.User
{
	public class CsvTestsFileDiscoverer<TModel> : ITestsDiscoverer<TModel, CsvFileTestcase> where TModel : IProgramModel
	{
		private readonly IUserRunConfigurationProvider runConfigurationProvider;

		public CsvTestsFileDiscoverer(IUserRunConfigurationProvider runConfigurationProvider)
		{
			this.runConfigurationProvider = runConfigurationProvider;
		}

		public Task<IList<CsvFileTestcase>> GetTestCasesForModel(TModel model, Func<CsvFileTestcase, bool> filterFunction, CancellationToken token)
		{
			var csvFile = runConfigurationProvider.CsvTestsFile;
			if (!File.Exists(csvFile))
			{
				throw new ArgumentException($"The CSV file '{csvFile}' does not exist!");
			}

			IList<CsvFileTestcase> tests = new List<CsvFileTestcase>();

			foreach (string line in File.ReadAllLines(csvFile))
			{
				token.ThrowIfCancellationRequested();

				string testName = line.Substring(0, line.IndexOf(';'));
				string linkedClass = line.Substring(line.IndexOf(';') + 1);

				var testCase = new CsvFileTestcase
				{
					Id = testName,
					AssociatedClass = linkedClass
				};

				if (filterFunction(testCase))
				{
					tests.Add(testCase);
				}
			}

			return Task.FromResult(tests);
		}
	}
}