using System;
using System.IO;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    internal class MSTestConstants
    {
        internal static readonly string VstestPath = Path.Combine(
            Environment.GetEnvironmentVariable("VS140COMNTOOLS") ?? @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools",
            @"..\IDE\CommonExtensions\Microsoft\TestWindow");
        internal const string Vstestconsole = @"vstest.console.exe";
        internal const string MSTestAdapterPath = @"MSTestAdapter";



        internal const string TestMethodAttributeName = "TestMethodAttribute";
        internal const string TestCategoryAttributeName = "TestCategoryAttribute";
        internal const string IgnoreAttributeName = "IgnoreAttribute";

        internal const string TestResultsFolder = "TestResults";
    }
}