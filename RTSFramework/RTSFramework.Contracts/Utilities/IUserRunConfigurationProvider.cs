using System.Collections.Generic;

namespace RTSFramework.Contracts.Utilities
{
	public interface IUserRunConfigurationProvider
	{
		IList<string> IntendedChanges { get; set; }

		double TimeLimit { get; set; }
	}
}