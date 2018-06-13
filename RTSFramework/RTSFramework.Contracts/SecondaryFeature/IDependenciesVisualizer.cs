using RTSFramework.Contracts.Models;

namespace RTSFramework.Contracts.SecondaryFeature
{
	public interface IDependenciesVisualizer
	{
		VisualizationData GetDependenciesVisualization(ICorrespondenceModel correspondenceModel);
	}
}