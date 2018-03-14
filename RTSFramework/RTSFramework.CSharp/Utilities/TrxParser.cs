using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Utilities
{

    public static class TrxFileParser
    {
        public static MSTestExectionResult Parse(string filename, IEnumerable<MSTestTestcase> testCases)
        {
            try
            {
                XNamespace ns = @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
                var doc = XDocument.Load(filename);

                var testDefinitions = (from unitTest in doc.Descendants(ns + "UnitTest")
                                       select new
                                       {
                                           executionId = unitTest.Element(ns + "Execution")?.Attribute("id")?.Value,
                                           codeBase = unitTest.Element(ns + "TestMethod")?.Attribute("codeBase")?.Value,
                                           className = unitTest.Element(ns + "TestMethod")?.Attribute("className")?.Value,
                                           testName = unitTest.Element(ns + "TestMethod")?.Attribute("name")?.Value
                                       }
                             ).ToList();

                var results = from utr in doc.Descendants(ns + "UnitTestResult")
                    let executionId = utr.Attribute("executionId")?.Value
                    let message = utr.Descendants(ns + "Message").FirstOrDefault()
                    let stackTrace = utr.Descendants(ns + "StackTrace").FirstOrDefault()
                    let st = DateTime.Parse(utr.Attribute("startTime")?.Value).ToUniversalTime()
                    let et = DateTime.Parse(utr.Attribute("endTime")?.Value).ToUniversalTime()
                    join testDefinition in testDefinitions on executionId equals testDefinition.executionId
                    join testCase in testCases on testDefinition.className + "." + testDefinition.testName equals testCase.Id
                    select new MSTestTestResult
                    {
                        AssemblyPathName = testDefinition.codeBase,
                        FullClassName = testDefinition.className,
                        StartTime = st,
                        EndTime = et,
                        Outcome = ParseOutcome(utr.Attribute("outcome")?.Value),
                        TestName = testDefinition.testName,
                        ErrorMessage = message?.Value ?? "",
                        StackTrace = stackTrace?.Value ?? "",
                        DurationInSeconds = (et - st).TotalSeconds,
                        AssociatedTestCase = testCase
                    };
                var executionResult = new MSTestExectionResult();
                executionResult.TestcasesResults.AddRange(results);

                var collectorElement =
                (from collectors in doc.Descendants(ns + "Collector")
                    let uri = collectors.Attribute("uri")?.Value
                    where uri == "datacollector://microsoft/CodeCoverage/2.0"
                    select collectors).SingleOrDefault();

                string fileName = collectorElement?.Descendants(ns + "A").SingleOrDefault()?.Attribute("href")?.Value;
                executionResult.CodeCoverageFile = fileName;

                return executionResult;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error while parsing Trx file '{filename}'", ex);
            }
        }

        //TODO Move closer to testcaseresulttype
        private static TestCaseResultType ParseOutcome(string outcome)
        {
            TestCaseResultType enumValue;
            if (!Enum.TryParse(outcome, true, out enumValue))
            {
                enumValue = TestCaseResultType.Failed;
            }

            return enumValue;
        }
    }
}