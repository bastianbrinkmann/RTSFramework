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
	public class OpenCoverXmlCoverageAdapter : IArtefactAdapter<MSTestExecutionResultParameters, CoverageData>
	{
		public CoverageData Parse(MSTestExecutionResultParameters resultParameters)
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

					return new CoverageData(coverageEntries);
				}
			}
		}

		private HashSet<CoverageDataEntry> GetCollectedCoverageEntries(CoverageSession session, Dictionary<uint, MSTestTestcase> coverageIdsTestCases)
		{
			var coverageEntries = new HashSet<CoverageDataEntry>();

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

								if (!coverageEntries.Any(
									x => x.ClassName == className.FullName &&
										 x.FileName == filePath &&
										 x.TestCaseId == associatedTestcase.Id))
								{
									coverageEntries.Add(new CoverageDataEntry
									{
										ClassName = className.FullName,
										FileName = filePath,
										TestCaseId = associatedTestcase.Id
									});
								}
							}
						}
					}
				}
			}
			return coverageEntries;
		}

		public void Unparse(CoverageData model, MSTestExecutionResultParameters artefact)
		{
			throw new System.NotImplementedException();
		}
	}
}