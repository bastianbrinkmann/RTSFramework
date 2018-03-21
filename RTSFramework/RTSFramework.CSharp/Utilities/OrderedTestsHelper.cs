using System;

namespace RTSFramework.Concrete.CSharp.Utilities
{
    public static class OrderedTestsHelper
    {
        public static int GetTestNumber(string line)
        {
            var beforeDash = line.Substring(0, line.IndexOf("-", StringComparison.Ordinal));

            string numberAsString = beforeDash;
            if (beforeDash.Contains(" "))
            {
                numberAsString = beforeDash.Substring(beforeDash.LastIndexOf(" ", StringComparison.Ordinal) + 1);
            }

            return int.Parse(numberAsString);
        }
    }
}