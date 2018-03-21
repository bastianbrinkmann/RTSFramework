using System.Collections.Generic;

namespace RTSFramework.Contracts.Artefacts
{
	public interface ITestCase
	{
		string Id { get; }

		List<string> Categories { get; }
	}
}