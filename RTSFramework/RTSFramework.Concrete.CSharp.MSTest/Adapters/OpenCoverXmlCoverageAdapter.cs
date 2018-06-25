using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using OpenCover.Framework.Model;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
	public class OpenCoverXmlCoverageAdapter : IArtefactAdapter<MSTestExecutionResultParameters, CorrespondenceLinks>
	{
		public GranularityLevel GranularityLevel { get; set; }

		public CorrespondenceLinks Parse(MSTestExecutionResultParameters resultParameters)
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

					var coverageEntries = GetCollectedCoverageEntries(session, coverageIdsTestCases);

					return new CorrespondenceLinks(coverageEntries);
				}
			}
		}

		private HashSet<Tuple<string, string>> GetCollectedCoverageEntries(CoverageSession session, Dictionary<uint, MSTestTestcase> coverageIdsTestCases)
		{
			var coverageEntries = new HashSet<Tuple<string, string>>();

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

				foreach (var className in module.Classes)
				{
					if (className.Methods == null)
					{
						continue;
					}

					foreach (var method in className.Methods)
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
								
								if (GranularityLevel == GranularityLevel.Class)
								{
									if (!coverageEntries.Any(x => x.Item1 == associatedTestcase.Id && x.Item2 == className.FullName))
									{
										coverageEntries.Add(new Tuple<string, string>(associatedTestcase.Id, className.FullName));
									}
								}/*TODO Granularity Level File
								else if (GranularityLevel == GranularityLevel.File)
								{
									if (!coverageEntries.Any(x => x.Item1 == associatedTestcase.Id && x.Item2 == filePath))
									{
										coverageEntries.Add(new Tuple<string, string>(associatedTestcase.Id, filePath));
									}
								}*/
							}
						}
					}
				}
			}
			return coverageEntries;
		}

		public MSTestExecutionResultParameters Unparse(CorrespondenceLinks model, MSTestExecutionResultParameters artefact)
		{
			throw new NotImplementedException();
		}
	}
}
 
 
 