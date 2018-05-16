﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RTSFramework.Concrete.CSharp.Core.Models;

namespace RTSFramework.Concrete.CSharp.MSTest.Models
{
	public class MSTestTestcase : ICSharpTestcase
    {
		public string Id { get; set; }

		public string Name { get; set; }

        public string AssociatedClass { get; set; }

        public bool Ignored { get; set; }

        public List<string> Categories { get; } = new List<string>();

		public string AssemblyPath { get; set; }

		public TestCase VsTestTestCase { get; set; }

		public bool IsChildTestCase { get; set; }

	    public Func<IList<string>> GetResponsibleChangesForLastImpact { get; set; }
    }
}