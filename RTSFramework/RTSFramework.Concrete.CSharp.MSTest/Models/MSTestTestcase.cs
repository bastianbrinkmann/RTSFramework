using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Core.Artefacts;
using RTSFramework.Concrete.CSharp.Core.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
	public class MSTestTestcase : ICSharpTestcase
    {
		public MSTestTestcase(string id, string assemblyPath, string name)
		{
			Id = id;
			AssemblyPath = assemblyPath;
			Name = name;
		}

		public string Id { get; }

		public string Name { get; }

        public bool Ignored { get; set; }

        public List<string> Categories { get; } = new List<string>();

		public string AssemblyPath { get; }
    }
}