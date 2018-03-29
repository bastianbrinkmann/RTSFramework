using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

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

        public Models.CorrespondenceModel GetCorrespondenceModel(string versionId, GranularityLevel granularityLevel)
        {
            Models.CorrespondenceModel model = correspondenceModels.SingleOrDefault(x => x.ProgramVersionId == versionId && x.GranularityLevel == granularityLevel);

            if (model == null)
            {
                var artefact = GetFile(versionId, granularityLevel);
                model = correspondenceModelAdapter.Parse(artefact);
                if (model == null)
                {
                    model = new Models.CorrespondenceModel { ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName), GranularityLevel = granularityLevel};
                }
                correspondenceModels.Add(model);
            }

            return model;
        }

        public void UpdateCorrespondenceModel<TTc>(CoverageData coverageData, string oldVersionId, string newVersionId, GranularityLevel granularityLevel, IEnumerable<TTc> allTests) where TTc : ITestCase
        {
            var oldModel = GetCorrespondenceModel(oldVersionId, granularityLevel);
            var newModel = oldModel.CloneModel(newVersionId);
            newModel.UpdateByNewLinks(GetLinksByCoverageData(coverageData, granularityLevel));
            newModel.RemoveDeletedTests(allTests.Select(x => x.Id));

            UpdateCorrespondenceModel(newModel);
        }

        private Dictionary<string, HashSet<string>> GetLinksByCoverageData(CoverageData coverageData, GranularityLevel granularityLevel)
        {
            var links = coverageData.CoverageDataEntries.Select(x => x.TestCaseId).Distinct().ToDictionary(x => x, x => new HashSet<string>());

            if (granularityLevel == GranularityLevel.Class)
            {
                foreach (var coverageEntry in coverageData.CoverageDataEntries)
                {
                    if (!links[coverageEntry.TestCaseId].Contains(coverageEntry.ClassName))
                    {
                        links[coverageEntry.TestCaseId].Add(coverageEntry.ClassName);
                    }
                }
            }
            else
            {

                foreach (var coverageEntry in coverageData.CoverageDataEntries)
                {
                    if (!links[coverageEntry.TestCaseId].Contains(coverageEntry.FileName))
                    {
                        links[coverageEntry.TestCaseId].Add(coverageEntry.FileName);
                    }
                }
            }

            return links;
        }

        private void UpdateCorrespondenceModel(Models.CorrespondenceModel model)
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