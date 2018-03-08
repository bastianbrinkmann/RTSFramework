using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class CSharpProgram : IProgram
    {
        public string SolutionPath { get; }

        public CSharpProgram(string solutionPath)
        {
            SolutionPath = solutionPath;
        }
    }
}