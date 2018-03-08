using System.Collections;
using System.Collections.Generic;
using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Contracts
{
    public interface IRTSApproach<TD, TPe, TTc> where TD : IDelta<TPe> where TTc : ITestCase where TPe : IProgramElement
    {
        IEnumerable<TTc> PerformRTS(IEnumerable<TTc> testCases, TD delta);
    }
}