using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.Reporting
{
    public class CsvTestsReporter<TTestCase, TDelta, TModel> : ITestProcessor<TTestCase, FileProcessingResult, TDelta, TModel> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
    {
        public Task<FileProcessingResult> ProcessTests(IList<TTestCase> impactedTests, IList<TTestCase> allTests, TDelta impactedForDelta, CancellationToken cancellationToken)
		{
            FileInfo csvFile = new FileInfo("Results.csv");
			var result = new FileProcessingResult();

            using (var stream = csvFile.OpenWrite())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("Tests:");
                    writer.WriteLine("Name;Categories");
                    foreach (var test in impactedTests)
                    {
						cancellationToken.ThrowIfCancellationRequested();
						writer.WriteLine($"{test.Id};" + string.Join(" ", test.Categories));
                    }
                }
            }
	        result.FilePath = csvFile.FullName;

			return Task.FromResult(result);
		}
    }
}
