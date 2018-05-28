using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
	public class CorrespondenceModelManager<TModel> where TModel : IProgramModel
	{
		private readonly IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter;
		private readonly List<Models.CorrespondenceModel> correspondenceModels = new List<Models.CorrespondenceModel>();
		private const string CorrespondenceModelsStoragePlace = "CorrespondenceModels";

		public CorrespondenceModelManager(IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter)
		{
			this.correspondenceModelAdapter = correspondenceModelAdapter;
		}

		public Task<Models.CorrespondenceModel> GetCorrespondenceModel(TModel programModel)
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

			return Task.FromResult(model);
		}

		public Task PersistCorrespondenceModel(Models.CorrespondenceModel model)
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

			return Task.CompletedTask;
		}

		private static FileInfo GetFile(string programVersionId, GranularityLevel granularityLevel)
		{
			return new FileInfo(Path.Combine(CorrespondenceModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{Uri.EscapeUriString(granularityLevel.ToString())}{JsonCorrespondenceModelAdapter.FileExtension}"));
		}
	}
}