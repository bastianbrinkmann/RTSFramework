using System;
using System.Collections.Generic;

namespace RTSFramework.Concrete.CSharp.Artefacts
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