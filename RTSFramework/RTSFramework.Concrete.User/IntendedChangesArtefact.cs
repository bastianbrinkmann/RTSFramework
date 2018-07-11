using System.Collections.Generic;
using RTSFramework.Concrete.User.Models;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.User
{
	public class IntendedChangesArtefact
	{
		public IList<string> IntendedChanges { get; set; } = new List<string>();

		public FilesProgramModel ProgramModel { get; set; }
	}
}