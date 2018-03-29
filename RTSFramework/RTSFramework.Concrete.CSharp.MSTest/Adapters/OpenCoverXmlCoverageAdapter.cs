using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using OpenCover.Framework.Model;
using RTSFramework.Concrete.CSharp.Core.Artefacts;
using RTSFramework.Concrete.CSharp.MSTest.Utilities;
using RTSFramework.Contracts.Artefacts;
using RTSFramework.Core;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
    public class OpenCoverXmlCoverageAdapter : IArtefactAdapter<MSTestExecutionResultParameters, ICoverageData>
    {
        public ICoverageData Parse(MSTestExecutionResultParameters resultParameters)
        {
            var serializer = new XmlSerializer(typeof(CoverageSession),
                                                    new[] { typeof(Module), typeof(OpenCover.Framework.Model.File), typeof(Class) });
            using (var stream = new FileStream(resultParameters.File.FullName, FileMode.Open))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding()))
                {
                    var session = (CoverageSession)serializer.Deserialize(reader);

                    //Determine Ids of testcases
                    var coverageIdsTestCases = new Dictionary<uint, MSTestTestcase>();

                    foreach (var module in session.Modules)
                    {
                        if (module.TrackedMethods != null)
                        {
                            foreach (var trackedMethod in module.TrackedMethods)
                            {
                                var fullName = trackedMethod.FullName.Replace("::", ".");

                                var associatedTest = resultParameters.ExecutedTestcases.SingleOrDefault(x => fullName.Contains($" {x.Id}()"));

                                coverageIdsTestCases.Add(trackedMethod.UniqueId, associatedTest);
                            }
                        }
                    }

                    var testCasesToElements = new Dictionary<string, HashSet<string>>(resultParameters.ExecutedTestcases.ToDictionary(
                            x => x.Id, x => new HashSet<string>()));

                    foreach (var module in session.Modules)
                    {
                        var files = new Dictionary<uint, string>();

                        
                        //Determine Ids of files
                        if (module.Files == null || module.Classes == null)
                        {
                            continue;
                        }

                        foreach (var file in module.Files)
                        {
                            files.Add(file.UniqueId, file.FullPath);
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
	                            var filePath = files[fileId];

                                foreach (var sequencePoint in method.SequencePoints)
                                {
                                    if (sequencePoint.TrackedMethodRefs == null)
                                    {
                                        continue;
                                    }

                                    foreach (var methodRef in sequencePoint.TrackedMethodRefs)
                                    {
	                                    var associatedTestcase = coverageIdsTestCases[methodRef.UniqueId];


                                        if (!testCasesToElements[associatedTestcase.Id].Contains(filePath))
                                        {
                                            testCasesToElements[associatedTestcase.Id].Add(filePath);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return new CoverageData {TransitiveClosureTestsToProgramElements = testCasesToElements};
                }
            }
        }

        public void Unparse(ICoverageData model, MSTestExecutionResultParameters artefact)
        {
            throw new System.NotImplementedException();
        }
    }
}