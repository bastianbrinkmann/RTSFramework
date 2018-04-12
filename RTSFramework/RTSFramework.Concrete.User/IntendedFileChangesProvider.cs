using System.Collections.Generic;

namespace RTSFramework.Concrete.User
{
	public class IntendedFileChangesProvider : IIntendedChangesProvider
	{
		public IList<string> IntendedChanges { get; set; } = new List<string>();
	}
}