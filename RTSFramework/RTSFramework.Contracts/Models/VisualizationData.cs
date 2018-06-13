using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public class VisualizationData
	{
		public Dictionary<string, HashSet<string>> LinksToVisualize { get; set; }
	}
}