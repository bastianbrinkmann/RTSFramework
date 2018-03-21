﻿using RTSFramework.Contracts.Artefacts;

namespace RTSFramework.Concrete.CSharp.Artefacts
{
    public class CSharpProgramModel : IProgramModel
    {
        public string VersionId { get; set; }
        public string SolutionPath { get; set; }
    }
}