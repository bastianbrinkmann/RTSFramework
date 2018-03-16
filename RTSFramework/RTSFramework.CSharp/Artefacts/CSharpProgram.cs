using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class CSharpProgram : IProgramModel
    {
        public string SolutionPath { get; }

        public CSharpProgram(string solutionPath)
        {
            SolutionPath = solutionPath;
        }

        public string VersionId { get; }
    }
}