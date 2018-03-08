using Microsoft.CodeAnalysis;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
	public interface ICSharpProgramElement : IProgramElement
	{
		string Id { get; }
	}
}