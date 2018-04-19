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

		//TestProperties
	    internal const string PropertyTestClassName = "MSTestDiscoverer.TestClassName";
		internal const string PropertyIsEnabled = "MSTestDiscoverer.IsEnabled";
		internal const string PropertyTestCategory = "MSTestDiscoverer.TestCategory";

	}
}