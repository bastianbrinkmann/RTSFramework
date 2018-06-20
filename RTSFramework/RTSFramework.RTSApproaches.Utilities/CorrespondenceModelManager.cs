using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using Unity.Interception.Utilities;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
	public class CorrespondenceModelManager<TModel> where TModel : IProgramModel
	{
		private readonly IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter;
		private const string CorrespondenceModelsStoragePlace = "CorrespondenceModels";

		public CorrespondenceModelManager(IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter)
		{
			this.correspondenceModelAdapter = correspondenceModelAdapter;
		}

		public Models.CorrespondenceModel GetCorrespondenceModel(TModel programModel)
		{
			var artefact = GetFile(programModel.VersionId, programModel.GranularityLevel);

			var defaultModel = new Models.CorrespondenceModel
			{
				ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName),
				GranularityLevel = programModel.GranularityLevel
			};


			var model = correspondenceModelAdapter.Parse(artefact) ?? defaultModel;

			return model;
		}

		public void UpdateCorrespondenceModel<TDelta>(CoverageData coverageData, TDelta currentDelta, IEnumerable<string> deletedTests, IEnumerable<string> failedTests)
			where TDelta : IDelta<TModel>
		{
			var oldCorrespondenceModel = GetCorrespondenceModel(currentDelta.OldModel);
			var newCorrespondenceModel = CloneModel(oldCorrespondenceModel, currentDelta.NewModel.VersionId);
			UpdateByNewLinks(newCorrespondenceModel, GetLinksByCoverageData(coverageData, currentDelta.NewModel));
			RemoveDeletedTests(newCorrespondenceModel, deletedTests);
			RemoveFailedTests(newCorrespondenceModel, failedTests);

			PersistCorrespondenceModel(newCorrespondenceModel);
		}

		private Models.CorrespondenceModel CloneModel(Models.CorrespondenceModel correspondenceModel, string newId)
		{
			var clone = new Dictionary<string, HashSet<string>>();
			foreach (KeyValuePair<string, HashSet<string>> testcaseRelatedElements in correspondenceModel.CorrespondenceModelLinks)
			{
				clone.Add(testcaseRelatedElements.Key, new HashSet<string>(testcaseRelatedElements.Value));
			}

			return new Models.CorrespondenceModel { ProgramVersionId = newId, CorrespondenceModelLinks = clone, GranularityLevel = correspondenceModel.GranularityLevel };
		}

		private void UpdateByNewLinks(Models.CorrespondenceModel correspondenceModel, Dictionary<string, HashSet<string>> newLinks)
		{
			foreach (KeyValuePair<string, HashSet<string>> linksForTestcase in newLinks)
			{
				if (!correspondenceModel.CorrespondenceModelLinks.ContainsKey(linksForTestcase.Key))
				{
					correspondenceModel.CorrespondenceModelLinks.Add(linksForTestcase.Key, linksForTestcase.Value);
				}
				else
				{
					correspondenceModel.CorrespondenceModelLinks[linksForTestcase.Key] = linksForTestcase.Value;
				}
			}
		}

		private void RemoveFailedTests(Models.CorrespondenceModel correspondenceModel, IEnumerable<string> failedTests)
		{
			failedTests.ForEach(x => correspondenceModel.CorrespondenceModelLinks.Remove(x));
		}

		private void RemoveDeletedTests(Models.CorrespondenceModel correspondenceModel, IEnumerable<string> deletedTests)
		{
			deletedTests.ForEach(x => correspondenceModel.CorrespondenceModelLinks.Remove(x));
		}

		private Dictionary<string, HashSet<string>> GetLinksByCoverageData(CoverageData coverageData, IProgramModel targetModel)
		{
			var links = coverageData.CoverageDataEntries.Select(x => x.Item1).Distinct().ToDictionary(x => x, x => new HashSet<string>());

			foreach (var coverageEntry in coverageData.CoverageDataEntries)
			{
				if (targetModel.GranularityLevel == GranularityLevel.Class)
				{
					if (!links[coverageEntry.Item1].Contains(coverageEntry.Item2))
					{
						links[coverageEntry.Item1].Add(coverageEntry.Item2);
					}
				}
				/* TODO Granularity Level File
				 * 
				 * else if(targetModel.GranularityLevel == GranularityLevel.File)
				{
					if (!coverageEntry.Item2.EndsWith(".cs"))
					{
						continue;
					}
					var relativePath = RelativePathHelper.GetRelativePath(targetModel, coverageEntry.Item2);
					if (!links[coverageEntry.Item1].Contains(relativePath))
					{
						links[coverageEntry.Item1].Add(relativePath);
					}
				}*/
			}
			return links;
		}

		private void PersistCorrespondenceModel(Models.CorrespondenceModel model)
		{
			if (!Directory.Exists(CorrespondenceModelsStoragePlace))
			{
				Directory.CreateDirectory(CorrespondenceModelsStoragePlace);
			}

			var artefact = GetFile(model.ProgramVersionId, model.GranularityLevel);
			correspondenceModelAdapter.Unparse(model, artefact);
		}

		private static FileInfo GetFile(string programVersionId, GranularityLevel granularityLevel)
		{
			return new FileInfo(Path.GetFullPath(Path.Combine(CorrespondenceModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{Uri.EscapeUriString(granularityLevel.ToString())}{JsonCorrespondenceModelAdapter.FileExtension}")));
		}
	}
}