using System.Collections.Generic;
using System.Linq;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Contracts.SecondaryFeature;

namespace RTSFramework.Concrete.CSharp.Roslyn.ResponsibleChanges
{
	public class ClassLevelResponsibleChangesReporter<TTestCase, TModel, TModelElement> : IResponsibleChangesReporter<TTestCase, TModel, StructuralDelta<TModel, TModelElement>>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TModelElement : IProgramModelElement
	{
		private readonly IDeltaAdapter<StructuralDelta<TModel, TModelElement>, StructuralDelta<TModel, CSharpClassElement>, TModel> deltaAdapter;
		public ClassLevelResponsibleChangesReporter(IDeltaAdapter<StructuralDelta<TModel, TModelElement>, StructuralDelta<TModel, CSharpClassElement>, TModel> deltaAdapter)
		{
			this.deltaAdapter = deltaAdapter;
		}

		public List<string> GetResponsibleChanges(ICorrespondenceModel correspondenceModel, TTestCase testCase, StructuralDelta<TModel, TModelElement> delta)
		{
			var selectionDelta = deltaAdapter.Convert(delta);

			if (correspondenceModel == null)
			{
				return new List<string>(new[] { "Responsible changes unkown!" });
			}

			if (correspondenceModel.CorrespondenceModelLinks.ContainsKey(testCase.Id))
			{
				var linksOfTestcase = correspondenceModel.CorrespondenceModelLinks[testCase.Id];
				return linksOfTestcase.Where(x => selectionDelta.AddedElements.Any(y => y.Id == x) ||
												  selectionDelta.ChangedElements.Any(y => y.Id == x) ||
												  selectionDelta.DeletedElements.Any(y => y.Id == x)).ToList();
			}

			return new List<string>();
		}
	}
}