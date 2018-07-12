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
		public List<string> AssociatedClasses { get; set; }
		public bool IsChildTestCase => false;
		public Func<IList<string>> GetResponsibleChangesForLastImpact { get; set; }

		public override bool Equals(object obj)
		{
			CsvFileTestcase other = obj as CsvFileTestcase;
			if (other != null)
			{
				return Id.Equals(other.Id);
			}

			return false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Id?.GetHashCode() ?? 0;
				return hashCode;
			}
		}
	}
}