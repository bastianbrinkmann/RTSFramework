using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class CsvTestsReporter<TTestCase> : ITestProcessor<TTestCase, FileProcessingResult> where TTestCase : ITestCase
    {
        public Task<FileProcessingResult> ProcessTests(IEnumerable<TTestCase> tests, CancellationToken cancellationToken = default(CancellationToken))
        {
            FileInfo csvFile = new FileInfo("Results.csv");
			var result = new FileProcessingResult();

            using (var stream = csvFile.OpenWrite())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("Tests:");
                    writer.WriteLine("Name;Categories");
                    foreach (var test in tests)
                    {
	                    if (cancellationToken.IsCancellationRequested)
	                    {
		                    return Task.FromResult(result);
	                    }
                        writer.WriteLine($"{test.Id};" + string.Join(",", test.Categories));
                    }
                }
            }
	        result.FilePath = csvFile.FullName;

			return Task.FromResult(result);
		}
    }
}
