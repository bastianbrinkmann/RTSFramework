using System.Collections.Generic;
using RTSFramework.Concrete.User.Models;

namespace RTSFramework.Concrete.User
{
	public class IntendedChangesArtefact
	{
		public IList<string> IntendedChanges { get; set; } = new List<string>();

		public LocalProgramModel LocalProgramModel { get; set; }
	}
}