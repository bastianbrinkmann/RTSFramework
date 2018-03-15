using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using OpenCover.Framework.Model;
using RTSFramework.Concrete.CSharp.Artefacts;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp.Utilities
{
    public static class OpenCoverXmlParser
    {


        public static ICoverageData Parse(string filename, IEnumerable<string> sources, IEnumerable<MSTestTestcase> testcases)
        {
            var serializer = new XmlSerializer(typeof(CoverageSession),
                                                    new[] { typeof(Module), typeof(OpenCover.Framework.Model.File), typeof(Class) });
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding()))
                {
                    var session = (CoverageSession)serializer.Deserialize(reader);

                    var mstestcases = testcases as IList<MSTestTestcase> ?? testcases.ToList();
                    var testModules = session.Modules.Where(x => sources.Contains(x.ModulePath));

                    //Determine Ids of testcases
                    var coverageIdsTestCases = new List<(uint, MSTestTestcase)>();

                    foreach (var module in testModules)
                    {
                        foreach (var trackedMethod in module.TrackedMethods)
                        {
                            var fullName = trackedMethod.FullName.Replace("::", ".");

                            var associatedTest = mstestcases.SingleOrDefault(x => fullName.Contains(x.Id));

                            coverageIdsTestCases.Add((trackedMethod.UniqueId, associatedTest));
                        }
                    }

                    var testCasesToElements = new Dictionary<string, HashSet<string>>(mstestcases.ToDictionary(
                            x => x.Id, x => new HashSet<string>()));

                    foreach (var module in session.Modules)
                    {
                        var files = new List<(uint, string)>();

                        
                        //Determine Ids of files
                        if (module.Files == null || module.Classes == null)
                        {
                            continue;
                        }

                        foreach (var file in module.Files)
                        {
                            files.Add((file.UniqueId, file.FullPath));
                        }

                        foreach (var @class in module.Classes)
                        {
                            if (@class.Methods == null)
                            {
                                continue;
                            }

                            foreach (var method in @class.Methods)
                            {
                                if (method.FileRef == null || method.SequencePoints == null)
                                {
                                    continue;
                                }

                                var fileId = method.FileRef.UniqueId;
                                var filePath = files.Single(x => x.Item1 == fileId).Item2;

                                foreach (var sequencePoint in method.SequencePoints)
                                {
                                    if (sequencePoint.TrackedMethodRefs == null)
                                    {
                                        continue;
                                    }

                                    foreach (var methodRef in sequencePoint.TrackedMethodRefs)
                                    {
                                        var associatedTestcase = coverageIdsTestCases.Single(x => x.Item1 == methodRef.UniqueId).Item2;


                                        if (!testCasesToElements[associatedTestcase.Id].Contains(filePath))
                                        {
                                            testCasesToElements[associatedTestcase.Id].Add(filePath);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return new CoverageData {TestCaseToProgramElementsMap = testCasesToElements};
                }
            }
        }
    }
}