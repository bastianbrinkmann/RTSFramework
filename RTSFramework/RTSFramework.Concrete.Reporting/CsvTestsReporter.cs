using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class CsvTestsReporter<TTc> : ITestProcessor<TTc> where TTc : ITestCase
    {
        public Task ProcessTests(IEnumerable<TTc> tests, CancellationToken cancellationToken = default(CancellationToken))
        {
            FileInfo csvFile = new FileInfo("Results.csv");

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
		                    return Task.CompletedTask;
	                    }
                        writer.WriteLine($"{test.Id};" + string.Join(",", test.Categories));
                    }
                }
            }

			return Task.CompletedTask;
		}
    }
}
