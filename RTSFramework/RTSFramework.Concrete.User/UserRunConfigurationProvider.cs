using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.User
{
	public class UserRunConfigurationProvider : IUserRunConfigurationProvider
	{
		public IList<string> IntendedChanges { get; set; } = new List<string>();

		public double TimeLimit { get; set; }

		public string CsvTestsFile { get; set; }
		public bool DiscoverNewTests { get; set; }
	}
}