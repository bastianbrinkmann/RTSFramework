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

		public void UpdateCorrespondenceModel<TDelta>(CoverageData coverageData, TDelta currentDelta, IEnumerable<string> allTests, IEnumerable<string> failedTests)
			where TDelta : IDelta<TModel>
		{
			var oldModel = GetCorrespondenceModel(currentDelta.OldModel);
			var newModel = oldModel.CloneModel(currentDelta.NewModel.VersionId);
			newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, currentDelta.NewModel));
			newModel.RemoveDeletedTests(allTests);

			failedTests.ForEach(x => newModel.CorrespondenceModelLinks.Remove(x));

			PersistDataStructure(newModel);
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

		public void PersistDataStructure(Models.CorrespondenceModel model)
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