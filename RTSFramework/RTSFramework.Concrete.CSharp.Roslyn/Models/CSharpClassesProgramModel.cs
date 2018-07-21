using System;
using System.Collections.Generic;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Roslyn.Models
{
	public class CSharpClassesProgramModel : CSharpProgramModel
	{
		public Func<IList<CSharpClassElement>> GetClasses { get; set; }
	}
}