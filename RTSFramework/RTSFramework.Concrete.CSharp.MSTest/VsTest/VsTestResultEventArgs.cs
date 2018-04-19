using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace RTSFramework.Concrete.CSharp.MSTest.VsTest
{
	public class VsTestResultEventArgs : EventArgs
	{
		public VsTestResultEventArgs(TestResult testResult)
		{
			VsTestResult = testResult;
		}

		public TestResult VsTestResult { get; }
	}
}