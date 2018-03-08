using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
	public class MSTestTestcase : ICSharpTestcase
    {
		public MSTestTestcase(string id, string category)
		{
			Id = id;
			Category = category;
		}

		public string Id { get; }
		public string Category { get; }
	}
}