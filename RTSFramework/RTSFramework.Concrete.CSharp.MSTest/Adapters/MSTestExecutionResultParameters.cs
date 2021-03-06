﻿using System.Collections.Generic;
using System.IO;
using RTSFramework.Concrete.CSharp.MSTest.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Adapters
{
    public class MSTestExecutionResultParameters
    {
        public FileInfo File { get; set; }

        public List<MSTestTestcase> ExecutedTestcases { get; } = new List<MSTestTestcase>();
    }
}