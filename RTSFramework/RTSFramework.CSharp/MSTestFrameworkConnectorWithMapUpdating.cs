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
            var msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();
            var testsFullyQualifiedNames = msTestTestcases.Select(x => x.Id).ToList();

            var results = new List<ITestCaseResult<MSTestTestcase>>();


            foreach (string test in testsFullyQualifiedNames)
            {
                var arguments = BuildArguments(new List<string>{ test });
                arguments += " /Enablecodecoverage";

                ExecuteVsTestsByArguments(arguments);


                var trxFile = GetTrxFile();
                if (trxFile == null)
                {
                    throw new ArgumentException("Test Execution Failed as no trx file was created!");
                }
                var exectionResult = TrxFileParser.Parse(trxFile.FullName, msTestTestcases);

                results.AddRange(exectionResult.TestcasesResults);
               
                //TODO parse coverage file
            
                var parser = new MikeParser();

                var codecoverageFile = Path.Combine(TestResultsFolder, Path.GetFileNameWithoutExtension(trxFile.Name),  @"In", exectionResult.CodeCoverageFile);
                FileInfo info = new FileInfo(codecoverageFile);
                var project = parser.Parse(new[] { info.FullName });
                
                foreach (var package in project.GetPackages())
                {
                    foreach (var file in package.GetFiles())
                    {
                        if (file.Metrics.CoveredElements > 0)
                        {
                            //TODO hand over previous map, adjust map
                        }
                    }
                }

                trxFile.Delete();
            }

            return results;
        }
        
    }
}