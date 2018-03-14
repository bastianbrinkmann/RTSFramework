using System.Collections.Generic;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
	public class MSTestTestcase : ICSharpTestcase
    {
		public MSTestTestcase(string id)
		{
			Id = id;
		}

		public string Id { get; }

        public List<string> Categories { get; } = new List<string>();
    }
}