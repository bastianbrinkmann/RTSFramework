using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Contracts.Adapter
{
	public interface IDeltaAdapter<TSourceDelta, TTargetDelta, TProgramSource, TProgramTarget> 
		where TSourceDelta : IDelta<TProgramSource>
		where TTargetDelta : IDelta<TProgramTarget>
		where TProgramSource : IProgramModel
		where TProgramTarget : IProgramModel
	{
		TTargetDelta Convert(TSourceDelta deltaToConvert);
	}
}