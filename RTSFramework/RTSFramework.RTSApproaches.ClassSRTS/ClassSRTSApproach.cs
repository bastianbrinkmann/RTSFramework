﻿using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.RTSApproach;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    /// <summary>
    /// An extensive study of static regression test selection in modern software evolution
    /// https://dl.acm.org/citation.cfm?id=2950361
    /// </summary>
    public class ClassSRTSApproach<TP> : RTSApproachBase<TP, CSharpClassElement, MSTestTestcase> where TP: IProgramModel
    {
        public ClassSRTSApproach()
        {
            
        }

        public override void ExecuteRTS(IEnumerable<MSTestTestcase> testCases, StructuralDelta<TP, CSharpClassElement> delta)
        {
            
        }

        private void ExtendDelta()
        {
            
        }
    }
}