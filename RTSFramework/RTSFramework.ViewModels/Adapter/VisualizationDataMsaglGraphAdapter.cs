using Microsoft.Msagl.Drawing;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;

namespace RTSFramework.ViewModels.Adapter
{
	public class VisualizationDataMsaglGraphAdapter : IArtefactAdapter<Graph, VisualizationData>
	{
		public VisualizationData Parse(Graph artefact)
		{
			throw new System.NotImplementedException();
		}

		public Graph Unparse(VisualizationData model, Graph artefact = null)
		{
			if (model == null)
			{
				return null;
			}

			var graph = new Graph("Dependencies Graph");

			foreach (var testLinks in model.LinksToVisualize)
			{
				foreach (var linkedProgramElement in testLinks.Value)
				{
					graph.AddEdge(testLinks.Key, linkedProgramElement);
				}
			}

			return graph;
		}
	}
}