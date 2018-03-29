using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface ITestCase
	{
		string Id { get; }

		List<string> Categories { get; }
	}
}