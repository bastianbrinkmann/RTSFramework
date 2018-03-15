using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Concrete.CSharp.Utilities;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.RTSApproaches.Utilities;
using Unity.Attributes;

namespace RTSFramework.Concrete.CSharp
{
    public class MSTestFrameworkConnectorWithMapUpdating : MSTestFrameworkConnector, IAutomatedTestFrameworkWithMapUpdating<MSTestTestcase>
    {
        private string sourceVersionId, targetVersionId;

        public MSTestFrameworkConnectorWithMapUpdating(IEnumerable<string> sources) : base(sources)
        {
        }

        public override IEnumerable<ITestCaseResult<MSTestTestcase>> ExecuteTests(IEnumerable<MSTestTestcase> tests)
        {
            var msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();
            var testsFullyQualifiedNames = msTestTestcases.Select(x => x.Id).ToList();

            var results = new List<ITestCaseResult<MSTestTestcase>>();
            TestCasesToProgramMap map = DynamicMapDictionary.GetMapByVersionId(sourceVersionId).CloneMap(targetVersionId);
            
            foreach (string test in testsFullyQualifiedNames)
            {
                HashSet<string> currentTestCaseToProgramMap;

                if (!map.TestCaseToProgramElementsMap.TryGetValue(test, out currentTestCaseToProgramMap))
                {
                    currentTestCaseToProgramMap = new HashSet<string>();
                    map.TestCaseToProgramElementsMap[test] = currentTestCaseToProgramMap;
                }

                var arguments = BuildVsTestsArguments(new List<string>{ test });
                arguments += " /Enablecodecoverage";

                ExecuteVsTestsByArguments(arguments);

                //Parse TrxFile for results
                var trxFile = GetTrxFile();
                if (trxFile == null)
                {
                    throw new ArgumentException("Test Execution Failed as no trx file was created!");
                }
                var exectionResult = TrxFileParser.Parse(trxFile.FullName, msTestTestcases);

                results.AddRange(exectionResult.TestcasesResults);
               
                //Coverage file parsing
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
                            if (!currentTestCaseToProgramMap.Contains(file.Path))
                            {
                                currentTestCaseToProgramMap.Add(file.Path);
                            }
                        }
                    }
                }

                trxFile.Delete();
            }

            DynamicMapDictionary.UpdateMap(map);

            return results;
        }

        public void SetSourceAndTargetVersion(string sourceVersion, string targetVersion)
        {
            sourceVersionId = sourceVersion;
            targetVersionId = targetVersion;
        }
    }
}