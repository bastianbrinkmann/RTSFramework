using System.Collections;
using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
    public interface IRTSApproach<TD, TPe, TP, TTc> where TD : IDelta<TPe, TP> where TTc : ITestCase where TPe : IProgramElement where TP : IProgram
    {
        IEnumerable<TTc> PerformRTS(IEnumerable<TTc> testCases, TD delta);
    }
}