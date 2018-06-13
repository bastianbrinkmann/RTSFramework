using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.Controller
{
	public interface IArtefactBasedController<TVisualizationArtefact>
	{
		TVisualizationArtefact GetDependenciesVisualization();
	}
}