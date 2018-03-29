using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Core.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
	public class MSTestTestcase : ICSharpTestcase
    {
		public MSTestTestcase(string assemblyPath, string name, string className)
		{
		    Id = $"{className}.{name}"; 
			AssemblyPath = assemblyPath;
			Name = name;
		    FullClassName = className;
		}

		public string Id { get; }

		public string Name { get; }

        public string FullClassName { get; }

        public bool Ignored { get; set; }

        public List<string> Categories { get; } = new List<string>();

		public string AssemblyPath { get; }
    }
}