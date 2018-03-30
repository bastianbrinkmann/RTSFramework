﻿using System;
using System.Linq;
using System.Xml.Linq;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.MSTest.Utilities;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
    public class TrxFileMsTestExecutionResultAdapter : IArtefactAdapter<MSTestExecutionResultParameters, MSTestExectionResult>
    {
        public MSTestExectionResult Parse(MSTestExecutionResultParameters artefact)
        {
            try
            {
                XNamespace ns = @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
                var doc = XDocument.Load(artefact.File.FullName);

                var testDefinitions = (from unitTest in doc.Descendants(ns + "UnitTest")
                                       select new
                                       {
                                           executionId = unitTest.Element(ns + "Execution")?.Attribute("id")?.Value,
                                           testName = unitTest.Element(ns + "TestMethod")?.Attribute("name")?.Value
                                       }
                ).ToList();

                var results = (from utr in doc.Descendants(ns + "UnitTestResult")
                               let executionId = utr.Attribute("executionId")?.Value
                               let message = utr.Descendants(ns + "Message").FirstOrDefault()
                               let stackTrace = utr.Descendants(ns + "StackTrace").FirstOrDefault()
                               let st = DateTime.Parse(utr.Attribute("startTime")?.Value).ToUniversalTime()
                               let et = DateTime.Parse(utr.Attribute("endTime")?.Value).ToUniversalTime()
                               join testDefinition in testDefinitions on executionId equals testDefinition.executionId
                               select new
                               {
                                   StartTime = st,
                                   EndTime = et,
                                   Outcome = ParseOutcome(utr.Attribute("outcome")?.Value),
                                   ErrorMessage = message?.Value ?? "",
                                   StackTrace = stackTrace?.Value ?? "",
                                   DurationInSeconds = (et - st).TotalSeconds,
                                   Name = OrderedTestsHelper.GetTestName(testDefinition.testName)
                               }).ToList();

                var executionResult = new MSTestExectionResult();

                for (int i = 0, j = 0; i < artefact.ExecutedTestcases.Count; i++)
                {
                    var currentTestCase = artefact.ExecutedTestcases[i];
                    var result = results[j];

                    var currentResult = new MSTestTestResult
                    {
                        TestCaseId = currentTestCase.Id,
                        Outcome = result.Outcome,
                        DurationInSeconds = result.DurationInSeconds,
                        EndTime = result.EndTime,
                        ErrorMessage = result.ErrorMessage,
                        StackTrace = result.StackTrace,
                        StartTime = result.StartTime
                    };

                    j++;
                    if (j < results.Count)
                    {
                        var nextResult = results[j];
                        while (nextResult.Name.StartsWith(currentTestCase.Name + " (Data Row"))
                        {
                            var childrenResult = new MSTestTestResult
                            {
                                TestCaseId = currentTestCase.Id,
                                Outcome = nextResult.Outcome,
                                DurationInSeconds = nextResult.DurationInSeconds,
                                EndTime = nextResult.EndTime,
                                ErrorMessage = nextResult.ErrorMessage,
                                StackTrace = nextResult.StackTrace,
                                StartTime = nextResult.StartTime
                            };
                            currentResult.ChildrenResults.Add(childrenResult);


                            j++;
                            if (j >= results.Count)
                            {
                                break;
                            }
                            nextResult = results[j];
                        }
                    }


                    executionResult.TestcasesResults.Add(currentResult);
                }

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
                throw new ArgumentException($"Error while parsing Trx file '{artefact.File.FullName}'", ex);
            }
        }

        public void Unparse(MSTestExectionResult model, MSTestExecutionResultParameters artefact)
        {
            throw new NotImplementedException();
        }

        //TODO Move closer to testcaseresulttype
        private TestCaseResultType ParseOutcome(string outcome)
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