﻿using System;
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

		public Models.CorrespondenceModel GetCorrespondenceModel<TTestCase>(TModel programModel, TestsModel<TTestCase> testsModel)
			where TTestCase : ITestCase
		{
			string testType = typeof(TTestCase).Name;
			var artefact = GetFile(testType, programModel.VersionId);

			var defaultModel = new Models.CorrespondenceModel
			{
				ProgramVersionId = Path.GetFileNameWithoutExtension(artefact.FullName),
				TestType = testType
			};

			var model = correspondenceModelAdapter.Parse(artefact) ?? defaultModel;

			return model;
		}

		public void UpdateCorrespondenceModel<TProgramDelta, TTestCase>(CorrespondenceLinks correspondenceLinks, TProgramDelta programDelta, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta, IEnumerable<string> failedTests)
			where TProgramDelta : IDelta<TModel>
			where TTestCase : ITestCase
		{
			var oldCorrespondenceModel = GetCorrespondenceModel(programDelta.OldModel, testsDelta.OldModel);
			var newCorrespondenceModel = CloneModel(oldCorrespondenceModel, programDelta.NewModel.VersionId);
			UpdateByNewLinks(newCorrespondenceModel, ConvertLinks(correspondenceLinks));
			RemoveDeletedTests(newCorrespondenceModel, testsDelta);
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

			return new Models.CorrespondenceModel
			{
				ProgramVersionId = newId,
				CorrespondenceModelLinks = clone,
				TestType = correspondenceModel.TestType
			};
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

		private void RemoveDeletedTests<TTestCase>(Models.CorrespondenceModel correspondenceModel, StructuralDelta<TestsModel<TTestCase>, TTestCase> testsDelta)
			where TTestCase : ITestCase
		{
			testsDelta.DeletedElements.ForEach(x => correspondenceModel.CorrespondenceModelLinks.Remove(x.Id));
		}

		private Dictionary<string, HashSet<string>> ConvertLinks(CorrespondenceLinks correspondenceLinks)
		{
			var links = correspondenceLinks.Links.Select(x => x.Item1).Distinct().ToDictionary(x => x, x => new HashSet<string>());

			foreach (var coverageEntry in correspondenceLinks.Links)
			{
				if (!links[coverageEntry.Item1].Contains(coverageEntry.Item2))
				{
					links[coverageEntry.Item1].Add(coverageEntry.Item2);
				}
			}
			return links;
		}

		private void PersistCorrespondenceModel(Models.CorrespondenceModel model)
		{
			if (!Directory.Exists(CorrespondenceModelsStoragePlace))
			{
				Directory.CreateDirectory(CorrespondenceModelsStoragePlace);
			}

			var artefact = GetFile(model.TestType, model.ProgramVersionId);
			correspondenceModelAdapter.Unparse(model, artefact);
		}

		private static FileInfo GetFile(string testModelType, string programVersionId)
		{
			return new FileInfo(Path.GetFullPath(Path.Combine(CorrespondenceModelsStoragePlace, $"{testModelType}_{Uri.EscapeUriString(programVersionId)}{JsonCorrespondenceModelAdapter.FileExtension}")));
		}
	}
}