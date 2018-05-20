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

		internal static string DefaultRunSettings = "<RunSettings>" +
												   "<RunConfiguration>" +
												   "</RunConfiguration>" +
												   "<MSTest>" +
												   "<AssemblyResolution>" +
												   "<Directory path=\"{0}\" includeSubDirectories=\"false\"/>" +
												   "</AssemblyResolution>" +
												   "</MSTest>" +
												   "</RunSettings>";

		//TestProperties
		internal const string PropertyTestClassName = "MSTestDiscoverer.TestClassName";
		internal const string PropertyIsEnabled = "MSTestDiscoverer.IsEnabled";
		internal const string PropertyTestCategory = "MSTestDiscoverer.TestCategory";
		internal const string PropertyIsDataDriven = "MSTestDiscoverer.IsDataDriven";

		internal const string MsTestAssemblyName = "Microsoft.VisualStudio.QualityTools.UnitTestFramework";
		internal const string MsTestModuleName = "Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll";

		internal const string MsTestTestInitializeAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute";
		internal const string MsTestTestCleanupAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute";
	}
}