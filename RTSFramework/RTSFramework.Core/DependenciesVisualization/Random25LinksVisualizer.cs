using System.Collections.Generic;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.SecondaryFeature;

namespace RTSFramework.Core.DependenciesVisualization
{
	public class Random25LinksVisualizer : IDependenciesVisualizer
	{
		public VisualizationData GetDependenciesVisualization(ICorrespondenceModel correspondenceModel)
		{
			if (correspondenceModel == null)
			{
				return null;
			}

			int counter = 0;

			var linksToVisualize = new Dictionary<string, HashSet<string>>();

			foreach(var testLinks in correspondenceModel.CorrespondenceModelLinks)
			{
				foreach (var linkedProgramElement in testLinks.Value)
				{
					if (counter == 25)
					{
						return new VisualizationData
						{
							LinksToVisualize = linksToVisualize
						};
					}

					if (!linksToVisualize.ContainsKey(testLinks.Key))
					{
						linksToVisualize.Add(testLinks.Key, new HashSet<string>());
					}
					linksToVisualize[testLinks.Key].Add(linkedProgramElement);
					counter++;
				}
			}

			return new VisualizationData
			{
				LinksToVisualize = linksToVisualize
			};
		}
	}
}