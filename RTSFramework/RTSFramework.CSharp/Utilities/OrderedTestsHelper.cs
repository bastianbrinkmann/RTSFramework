using System;

namespace RTSFramework.Concrete.CSharp.Utilities
{
    public static class OrderedTestsHelper
    {
        public static int GetTestNumber(string line)
        {
            if (!line.Contains("-"))
            {
                return -1;
            }
            var beforeDash = line.Substring(0, line.IndexOf("-", StringComparison.Ordinal));

            string numberAsString = beforeDash;
            if (beforeDash.Contains(" "))
            {
                numberAsString = beforeDash.Substring(beforeDash.LastIndexOf(" ", StringComparison.Ordinal) + 1);
            }

            return int.Parse(numberAsString);
        }

        public static string GetTestName(string line)
        {
            if (!line.Contains(" "))
            {
                return "";
            }

            var afterFirstSpace = line.Substring(line.IndexOf(" ", StringComparison.Ordinal) + 1);

            string untilNextSpace = null;
            if (afterFirstSpace.Contains(" (testrun)"))
            {
                untilNextSpace = afterFirstSpace.Substring(0, afterFirstSpace.IndexOf(" (testrun)", StringComparison.Ordinal));
            }

            return untilNextSpace ?? afterFirstSpace;
        }
    }
}