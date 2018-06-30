using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.SecondaryFeature
{
	public interface IResponsibleChangesReporter<TTestCase, TModel, TDelta>
		where TTestCase : ITestCase
		where TModel : IProgramModel
		where TDelta : IDelta<TModel>
	{
		List<string> GetResponsibleChanges(ICorrespondenceModel correspondenceModel, TTestCase testCase, TDelta delta);
	}
}