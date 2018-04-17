using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.RTSApproach
{
	public interface IDynamicRTSApproach<TModel, TDelta, TTestCase, TCorrespondenceModel> : IRTSApproach<TModel, TDelta, TTestCase> where TTestCase : ITestCase where TDelta : IDelta<TModel> where TModel : IProgramModel
	{
		TCorrespondenceModel CorrespondenceModel { get; set; }
	}
}