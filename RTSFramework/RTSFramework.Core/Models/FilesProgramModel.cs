using System.Collections.Generic;

namespace RTSFramework.Core.Models
{
    public class FilesProgramModel : CSharpProgramModel
    {
		public List<FileElement> Files { get; } = new List<FileElement>();
	}
}