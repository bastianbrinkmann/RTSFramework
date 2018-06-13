using System.Collections.Generic;

namespace RTSFramework.Contracts
{
	public interface ICorrespondenceModel
	{
		Dictionary<string, HashSet<string>> CorrespondenceModelLinks { get; set; }
	}
}