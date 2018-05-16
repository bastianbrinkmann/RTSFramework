using System;
using System.Collections.Generic;

namespace RTSFramework.Contracts.Models
{
	public interface ITestCase
	{
		string Id { get; }

		List<string> Categories { get; }

		string Name { get; }

		string AssociatedClass { get; }

		bool IsChildTestCase { get; }

		Func<IList<string>> GetResponsibleChangesForLastImpact { get; set; }
	}
}