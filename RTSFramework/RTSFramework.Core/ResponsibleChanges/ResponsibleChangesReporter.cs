using System;
using System.Collections.Generic;
using System.Linq;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.SecondaryFeature;

namespace RTSFramework.Core.ResponsibleChanges
{
	public class ResponsibleChangesReporter<TTestCase, TModel, TModelElement> : IResponsibleChangesReporter<TTestCase, TModel, StructuralDelta<TModel, TModelElement>>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		public List<string> GetResponsibleChanges(ICorrespondenceModel correspondenceModel, TTestCase testCase, StructuralDelta<TModel, TModelElement> delta)
		{
			if (correspondenceModel == null)
			{
				return new List<string>(new[] { "Responsible changes unkown!" });
			}

			if (correspondenceModel.CorrespondenceModelLinks.ContainsKey(testCase.Id))
			{
				var linksOfTestcase = correspondenceModel.CorrespondenceModelLinks[testCase.Id];
				return linksOfTestcase.Where(x => delta.AddedElements.Any(y => y.Id == x) ||
												  delta.ChangedElements.Any(y => y.Id == x) ||
												  delta.DeletedElements.Any(y => y.Id == x)).ToList();
			}

			return new List<string>();
		}
	}
}