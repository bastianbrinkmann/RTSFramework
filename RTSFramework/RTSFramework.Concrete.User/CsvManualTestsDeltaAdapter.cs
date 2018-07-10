using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.User
{
	public class CsvManualTestsDeltaAdapter<TProgram, TProgramDelta> : ITestsDeltaAdapter<TProgram, TProgramDelta, CsvFileTestcase> where TProgram : IProgramModel where TProgramDelta : IDelta<TProgram>
	{
		private const string TestsModelsStoragePlace = "TestsModels";
		private const string TestTypeIdentifier = "CsvFile";

		private readonly IUserRunConfigurationProvider runConfigurationProvider;
		private readonly IArtefactAdapter<FileInfo, TestsModel<CsvFileTestcase>> testsModelAdapter;

		public CsvManualTestsDeltaAdapter(IUserRunConfigurationProvider runConfigurationProvider, 
			IArtefactAdapter<FileInfo, TestsModel<CsvFileTestcase>> testsModelAdapter)
		{
			this.runConfigurationProvider = runConfigurationProvider;
			this.testsModelAdapter = testsModelAdapter;
		}

		//TODO: Artefact Adapter
		public Task<StructuralDelta<TestsModel<CsvFileTestcase>, CsvFileTestcase>> GetTestsDelta(TProgramDelta programDelta, Func<CsvFileTestcase, bool> filterFunction, CancellationToken token)
		{
			var csvFile = runConfigurationProvider.CsvTestsFile;
			if (!File.Exists(csvFile))
			{
				throw new ArgumentException($"The CSV file '{csvFile}' does not exist!");
			}

			var oldTestsModel = testsModelAdapter.Parse(GetTestsStorage(programDelta.OldModel.VersionId));
			if (oldTestsModel == null)
			{
				oldTestsModel = new TestsModel<CsvFileTestcase>
				{
					TestSuite = new HashSet<CsvFileTestcase>(),
					VersionId = programDelta.OldModel.VersionId
				};
			}

			TestsModel<CsvFileTestcase> newTestsModel = new TestsModel<CsvFileTestcase>
			{
				TestSuite = new HashSet<CsvFileTestcase>(),
				VersionId = programDelta.NewModel.VersionId
			};

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
					newTestsModel.TestSuite.Add(testCase);
				}
			}
			testsModelAdapter.Unparse(newTestsModel, GetTestsStorage(programDelta.NewModel.VersionId));

			var testsDelta = new StructuralDelta<TestsModel<CsvFileTestcase>, CsvFileTestcase>(oldTestsModel, newTestsModel);
			testsDelta.AddedElements.AddRange(newTestsModel.TestSuite.Except(oldTestsModel.TestSuite));
			testsDelta.DeletedElements.AddRange(oldTestsModel.TestSuite.Except(newTestsModel.TestSuite));

			return Task.FromResult(testsDelta);
		}

		//TODO to super class
		private FileInfo GetTestsStorage(string programVersionId)
		{
			return new FileInfo(Path.GetFullPath(Path.Combine(TestsModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{TestTypeIdentifier}_{JsonTestsModelAdapter<CsvFileTestcase>.FileExtension}")));
		}
	}
}