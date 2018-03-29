using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.RTSApproaches.CorrespondenceModel
{
    public class CorrespondenceModelManager
    {
        private readonly IArtefactAdapter<FileInfo, CorrespondenceModel> correspondenceModelAdapter;
        private readonly List<CorrespondenceModel> correspondenceModels = new List<CorrespondenceModel>();
        private const string CorrespondenceModelsStoragePlace = "CorrespondenceModels";

        public CorrespondenceModelManager(IArtefactAdapter<FileInfo, CorrespondenceModel> correspondenceModelAdapter)
        {
            this.correspondenceModelAdapter = correspondenceModelAdapter;
        }

        public CorrespondenceModel GetCorrespondenceModel(string versionId)
        {
            CorrespondenceModel model = correspondenceModels.SingleOrDefault(x => x.ProgramVersionId == versionId);

            if (model == null)
            {
                var artefact = GetFile(versionId);
                model = correspondenceModelAdapter.Parse(artefact);
                correspondenceModels.Add(model);
            }

            return model;
        }

        public void UpdateCorrespondenceModel<TTc>(ICoverageData coverageData, string oldVersionId, string newVersionId, IEnumerable<TTc> allTests) where TTc : ITestCase
        {
            var oldModel = GetCorrespondenceModel(oldVersionId);
            var newModel = oldModel.CloneModel(newVersionId);
            newModel.UpdateByNewLinks(coverageData.TransitiveClosureTestsToProgramElements);
            newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

            UpdateCorrespondenceModel(newModel);
        }

        private void UpdateCorrespondenceModel(CorrespondenceModel model)
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

            var artefact = GetFile(model.ProgramVersionId);
            correspondenceModelAdapter.Parse(artefact);
            correspondenceModels.Add(model);
        }

        private static FileInfo GetFile(string programVersionId)
        {
            return new FileInfo(Path.Combine(CorrespondenceModelsStoragePlace, Uri.EscapeUriString(programVersionId) + JsonCorrespondenceModelAdapter.FileExtension));
        }
    }
}