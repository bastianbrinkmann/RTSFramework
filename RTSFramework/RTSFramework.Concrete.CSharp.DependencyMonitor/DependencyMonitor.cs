// Copyright (c) 2017, Marko Vasic
// Modifications Copyright (C) 2018 Bastian Brinkmann
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;

namespace RTSFramework.Concrete.CSharp.DependencyMonitor
{
	public static class DependencyMonitor
	{
		public const string ClassFullName = "RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor";

		private static string currentTestMethodName = string.Empty;

		private static Dictionary<string, HashSet<string>> Dependencies = new Dictionary<string, HashSet<string>>();

		public static string TypeMethodFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::T(System.String)";
		public static string TestMethodStartFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodStart(System.String)";
		public static string TestMethodEndFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodEnd()";

		static DependencyMonitor()
		{
			//Init?
		}

		public static void T(string typeWithFullPath)
		{
			if (Dependencies.ContainsKey(currentTestMethodName))
			{
				if (!Dependencies[currentTestMethodName].Contains(typeWithFullPath))
				{
					Dependencies[currentTestMethodName].Add(typeWithFullPath);
				}
			}
		}

		public static void TestMethodStart(string testMethod)
		{
			currentTestMethodName = testMethod;
			if (testMethod != null)
			{
				if (!Dependencies.ContainsKey(testMethod))
				{
					Dependencies.Add(testMethod, new HashSet<string>());
				}

				int dotIndex = testMethod.LastIndexOf('.');

				string className = testMethod.Substring(0, dotIndex);
				// always add test class into dependency list
				if (!Dependencies[testMethod].Contains(className))
				{
					Dependencies[testMethod].Add(className);
				}
			}
		}

		public static void TestMethodEnd()
		{
			using (var writer = File.AppendText("Testfile.txt"))
			{
				writer.WriteLine(currentTestMethodName);
				if (!Dependencies.ContainsKey(currentTestMethodName))
				{
					return;
				}

				foreach (var reference in Dependencies[currentTestMethodName])
				{
					writer.WriteLine(reference);
				}

				writer.WriteLine();
			}

			currentTestMethodName = string.Empty;
		}
	}
}
