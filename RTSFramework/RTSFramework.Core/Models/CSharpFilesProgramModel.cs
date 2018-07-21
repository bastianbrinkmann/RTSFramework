using System.Collections.Generic;

namespace RTSFramework.Core.Models
{
    public class CSharpFilesProgramModel : CSharpProgramModel
    {
		public List<CSharpFileElement> Files { get; } = new List<CSharpFileElement>();
	}
}