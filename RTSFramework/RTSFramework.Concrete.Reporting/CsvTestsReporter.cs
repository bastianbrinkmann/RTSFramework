using System.Collections.Generic;
using System.IO;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.Reporting
{
    public class CsvTestsReporter<TTc> : ITestProcessor<TTc> where TTc : ITestCase
    {
        public void ProcessTests(IEnumerable<TTc> tests)
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
                        writer.WriteLine($"{test.Id};" + string.Join(",", test.Categories));
                    }
                }
            }
        }
    }
}
