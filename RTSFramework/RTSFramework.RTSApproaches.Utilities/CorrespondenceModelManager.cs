using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Utilities;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
	public class CorrespondenceModelManager
	{
		private readonly IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter;
		private readonly List<Models.CorrespondenceModel> correspondenceModels = new List<Models.CorrespondenceModel>();
		private const string CorrespondenceModelsStoragePlace = "CorrespondenceModels";

		public CorrespondenceModelManager(IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter)
		{
			this.correspondenceModelAdapter = correspondenceModelAdapter;
		}

		public Models.CorrespondenceModel GetCorrespondenceModelOrDefault(IProgramModel programModel)
		{
			var artefact = GetFile(programModel.VersionId, programModel.GranularityLevel);

			var defaultModel = new Models.CorrespondenceModel
			{
				ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName),
				GranularityLevel = programModel.GranularityLevel
			};

			Models.CorrespondenceModel model = correspondenceModels.SingleOrDefault(x => x.ProgramVersionId == programModel.VersionId && x.GranularityLevel == programModel.GranularityLevel);

			if (model == null)
			{
				model = correspondenceModelAdapter.Parse(artefact) ?? defaultModel;

				correspondenceModels.Add(model);
			}

			return model;
		}

		private IProgramModel source, target;
		private List<string> allTestCaseIds;

		public void PrepareCorrespondenceModelCreation<TModel, TTestCase>(IDelta<TModel> delta, IEnumerable<TTestCase> allTests) where TModel : IProgramModel where TTestCase : ITestCase
		{
			source = delta.SourceModel;
			target = delta.TargetModel;
			allTestCaseIds = allTests.Select(x => x.Id).ToList();
		}

		public void CreateCorrespondenceModel(CoverageData coverageData) 
		{
			var oldModel = GetCorrespondenceModelOrDefault(source);
			var newModel = oldModel.CloneModel(target.VersionId);
			newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, target));
			newModel.RemoveDeletedTests(allTestCaseIds);

			PersistCorrespondenceModel(newModel);
		}

		private Dictionary<string, HashSet<string>> GetLinksByCoverageData(CoverageData coverageData, IProgramModel targetModel)
		{
			var links = coverageData.CoverageDataEntries.Select(x => x.TestCaseId).Distinct().ToDictionary(x => x, x => new HashSet<string>());

			foreach (var coverageEntry in coverageData.CoverageDataEntries)
			{
				if (targetModel.GranularityLevel == GranularityLevel.Class)
				{
					if (!links[coverageEntry.TestCaseId].Contains(coverageEntry.ClassName))
					{
						links[coverageEntry.TestCaseId].Add(coverageEntry.ClassName);
					}
					else
					{
						var relativePath = RelativePathHelper.GetRelativePath(targetModel, coverageEntry.FileName);
						if (!links[coverageEntry.TestCaseId].Contains(relativePath))
						{
							links[coverageEntry.TestCaseId].Add(relativePath);
						}
					}
				}
			}
			return links;
		}

		private void PersistCorrespondenceModel(Models.CorrespondenceModel model)
		{
			var currentModel = correspondenceModels.SingleOrDefault(x => x.ProgramVersionId == model.ProgramVersionId);

			if (currentModel != null)
			{
				correspondenceModels.Remove(currentModel);
			}

			if (!Directory.Exists(CorrespondenceModelsStoragePlace))
			{
				Directory.CreateDirectory(CorrespondenceModelsStoragePlace);
			}

			var artefact = GetFile(model.ProgramVersionId, model.GranularityLevel);
			correspondenceModelAdapter.Unparse(model, artefact);
			correspondenceModels.Add(model);
		}

		private static FileInfo GetFile(string programVersionId, GranularityLevel granularityLevel)
		{
			return new FileInfo(Path.Combine(CorrespondenceModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{Uri.EscapeUriString(granularityLevel.ToString())}{JsonCorrespondenceModelAdapter.FileExtension}"));
		}
	}
}