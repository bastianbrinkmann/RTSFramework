using System;
using System.IO;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class TestsCsvFileAdapter<TTestCase> : IArtefactAdapter<CsvFileArtefact, TestListResult<TTestCase>> where TTestCase : ITestCase
    {
		public TestListResult<TTestCase> Parse(CsvFileArtefact artefact)
		{
			throw new NotImplementedException();
		}

		public CsvFileArtefact Unparse(TestListResult<TTestCase> model, CsvFileArtefact artefact)
		{
			if (artefact == null)
			{
				artefact = new CsvFileArtefact {CsvFilePath = "Result.csv"};
			}

			FileInfo csvFile = new FileInfo(artefact.CsvFilePath);
			using (var stream = csvFile.OpenWrite())
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					writer.WriteLine("Tests:");
					writer.WriteLine("Name;Categories");
					foreach (var test in model.IdentifiedTests)
					{
						writer.WriteLine($"{test.Id};" + string.Join(" ", test.Categories));
					}
				}
			}

			return artefact;
		}
	}
}
