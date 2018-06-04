using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.RTSApproaches.Core.Contracts;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
	public class CorrespondenceModelManager<TModel> : IDataStructureProvider<Models.CorrespondenceModel, TModel> where TModel : IProgramModel
	{
		private readonly IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter;
		private const string CorrespondenceModelsStoragePlace = "CorrespondenceModels";

		public CorrespondenceModelManager(IArtefactAdapter<FileInfo, Models.CorrespondenceModel> correspondenceModelAdapter)
		{
			this.correspondenceModelAdapter = correspondenceModelAdapter;
		}

		public Task<Models.CorrespondenceModel> GetDataStructure(TModel programModel, CancellationToken cancellationToken)
		{
			var artefact = GetFile(programModel.VersionId, programModel.GranularityLevel);

			var defaultModel = new Models.CorrespondenceModel
			{
				ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName),
				GranularityLevel = programModel.GranularityLevel
			};


			var model = correspondenceModelAdapter.Parse(artefact) ?? defaultModel;

			return Task.FromResult(model);
		}

		public Task PersistDataStructure(Models.CorrespondenceModel model)
		{
			if (!Directory.Exists(CorrespondenceModelsStoragePlace))
			{
				Directory.CreateDirectory(CorrespondenceModelsStoragePlace);
			}

			var artefact = GetFile(model.ProgramVersionId, model.GranularityLevel);
			correspondenceModelAdapter.Unparse(model, artefact);

			return Task.CompletedTask;
		}

		private static FileInfo GetFile(string programVersionId, GranularityLevel granularityLevel)
		{
			return new FileInfo(Path.GetFullPath(Path.Combine(CorrespondenceModelsStoragePlace, $"{Uri.EscapeUriString(programVersionId)}_{Uri.EscapeUriString(granularityLevel.ToString())}{JsonCorrespondenceModelAdapter.FileExtension}")));
		}
	}
}