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

using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace RTSFramework.Concrete.CSharp.DependencyMonitor
{
	public static class DependencyMonitor
	{
		private static string currentTestMethodName = string.Empty;

		private static HashSet<string> dependencies;

		private const string DependenciesFolder = @"TestResults\Dependencies";

		private static string GetDependenciesFolder()
		{
			var assemblyFolder = Assembly.GetAssembly(typeof(DependencyMonitor)).Location;
			var directory = Path.GetDirectoryName(assemblyFolder)?? "";
			return Path.Combine(directory, DependenciesFolder);
		}

		static DependencyMonitor()
		{
			var dependenciesFolder = GetDependenciesFolder();

			if (!Directory.Exists(dependenciesFolder))
			{
				Directory.CreateDirectory(dependenciesFolder);
			}
		}

		public static void T(string typeWithFullPath)
		{
			if (dependencies != null && !dependencies.Contains(typeWithFullPath))
			{
				dependencies.Add(typeWithFullPath);
			}
		}

		public static void TestMethodStart()
		{
			dependencies = new HashSet<string>();
		}

		public static void TestMethodName(string testMethodIdentifier, string testClass)
		{
			currentTestMethodName = testMethodIdentifier;

			if (testMethodIdentifier != null)
			{
				//TODO Granularity Level File
				dependencies.Add(testClass);
			}
		}

		public static void TestMethodEnd()
		{
			var dependenciesFolder = GetDependenciesFolder();

			using (var fileStream = File.Create(Path.Combine(dependenciesFolder, currentTestMethodName + ".json")))
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
