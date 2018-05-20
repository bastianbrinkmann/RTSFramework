using System;
using System.Collections.Generic;
using RTSFramework.Contracts.Models;

namespace RTSFramework.Concrete.User.Models
{
	public class CsvFileTestcase : ITestCase
	{
		public string Id { get; set; }
		public List<string> Categories => new List<string>();
		public string Name => Id;
		public string AssociatedClass { get; set; }
		public bool IsChildTestCase => false;
		public Func<IList<string>> GetResponsibleChangesForLastImpact { get; set; }
	}
}