using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnectorWithMapUpdating : MSTestFrameworkConnector
    {

        public MSTestFrameworkConnectorWithMapUpdating(IEnumerable<string> sources) : base(sources)
        {
        }

        protected override IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTestsInternal(IEnumerable<MSTestTestcase> tests)
        {
            var testsFullyQualifiedNames = tests.Select(x => x.Id).ToList();

            var results = new List<ITestCaseResult<MSTestTestcase>>();


            foreach (string test in testsFullyQualifiedNames)
            {
                var arguments = BuildArguments(new List<string>{ test });
                arguments += " /Enablecodecoverage";

                ExecuteVsTestsByArguments(arguments);

                var executionResult = ParseVsTestsTrxAnswer();
                results.AddRange(executionResult.TestcasesResults);
               
                //TODO parse coverage file


            }

            return results;
        }
        
    }
}