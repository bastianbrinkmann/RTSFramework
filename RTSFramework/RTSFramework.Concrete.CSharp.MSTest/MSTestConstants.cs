using System;
using System.IO;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    internal class MSTestConstants
    {
	    internal static readonly string VstestPath = @"VsTest\Common7\IDE\Extensions\TestPlatform";
		internal const string Vstestconsole = @"vstest.console.exe";

        internal const string TestMethodAttributeName = "TestMethodAttribute";
        internal const string TestCategoryAttributeName = "TestCategoryAttribute";
        internal const string IgnoreAttributeName = "IgnoreAttribute";

        internal const string TestResultsFolder = "TestResults";
    }
}