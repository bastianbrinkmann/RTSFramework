﻿// Copyright (c) 2017, Marko Vasic
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

using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RTSFramework.Concrete.CSharp.DependencyMonitor
{
	public static class DependencyMonitor
	{
		public const string ClassFullName = "RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor";

		private static string currentTestMethodName = string.Empty;

		private static HashSet<string> dependencies;

		public static string TypeMethodFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::T(System.String)";
		public static string TestMethodStartFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodStart(System.String)";
		public static string TestMethodEndFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodEnd()";

		private const string DependenciesFolder = @"..\..\Dependencies\";

		static DependencyMonitor()
		{
			if (!Directory.Exists(DependenciesFolder))
			{
				Directory.CreateDirectory(DependenciesFolder);
			}
		}

		public static void T(string typeWithFullPath)
		{
			if (dependencies != null && !dependencies.Contains(typeWithFullPath))
			{
				dependencies.Add(typeWithFullPath);
			}
		}

		public static void TestMethodStart(string testMethod)
		{
			currentTestMethodName = testMethod;
			dependencies = new HashSet<string>();

			if (testMethod != null)
			{
				int dotIndex = testMethod.LastIndexOf('.');

				//TODO Granularity Level File
				string className = testMethod.Substring(0, dotIndex);
				dependencies.Add(className);
			}
		}

		public static void TestMethodEnd()
		{
			using (var fileStream = File.Create(DependenciesFolder + currentTestMethodName + ".json"))
			{
				using (StreamWriter writer = new StreamWriter(fileStream))
				{
					var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
					serializer.Serialize(writer, dependencies);
				}
			}

			currentTestMethodName = string.Empty;
			dependencies = null;
		}
	}
}