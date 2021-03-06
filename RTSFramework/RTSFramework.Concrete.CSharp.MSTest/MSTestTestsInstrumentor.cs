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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using Newtonsoft.Json;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestsInstrumentor<TModel> : ITestsInstrumentor<TModel, MSTestTestcase> where TModel : CSharpProgramModel
	{
		private const string MonoModuleTyp = "<Module>";
		private const string DependenciesFolder = @"TestResults\Dependencies";
		private const string MonitorAssemblyName = "RTSFramework.Concrete.CSharp.DependencyMonitor";
		private const string TestInitName = "RTSFramworkGeneratedTestInitMethod";
		private const string TestCleanupName = "RTSFramworkGeneratedTestCleanupMethod";

		private ModuleDefinition dependencyMonitorModule;
		private TypeDefinition dependencyMonitorType;
		private MethodReference testMethodStartedReference;
		private MethodReference testMethodNameReference;
		private MethodReference testMethodEndReference;
		private MethodReference typeVisitedMethodReference;

		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly ILoggingHelper loggingHelper;
		private readonly ISettingsProvider settingsProvider;

		private readonly Dictionary<string, string> classToFileNamesMapping = new Dictionary<string, string>();

		public MSTestTestsInstrumentor(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter,
			ILoggingHelper loggingHelper,
			ISettingsProvider settingsProvider)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.loggingHelper = loggingHelper;
			this.settingsProvider = settingsProvider;
		}

		private List<string> assemblies;
		private List<string> testAssemblies;
		private List<string> assemblyNames;
		private IList<MSTestTestcase> msTestTestcases;

		private const string DependencyMonitorClassFullName = "RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor";
		private const string TypeMethodFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::T(System.String)";

		private const string TestMethodStartFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodStart()";
		private const string TestMethodNameFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodName(System.String,System.String)";
		private const string TestMethodEndFullName = "System.Void RTSFramework.Concrete.CSharp.DependencyMonitor.DependencyMonitor::TestMethodEnd()";
		

		private List<Tuple<string, int>> testNamesToExecutionIds;

		public async Task Instrument(TModel toInstrument, IList<MSTestTestcase> tests, CancellationToken token)
		{
			dependencyMonitorModule = ModuleDefinition.ReadModule(Path.GetFullPath($"{MonitorAssemblyName}.dll"));
			dependencyMonitorType = dependencyMonitorModule.Types.Single(x => x.FullName == DependencyMonitorClassFullName);
			testMethodStartedReference = dependencyMonitorType.Methods.Single(x => x.FullName == TestMethodStartFullName);
			testMethodNameReference = dependencyMonitorType.Methods.Single(x => x.FullName == TestMethodNameFullName);
			testMethodEndReference = dependencyMonitorType.Methods.Single(x => x.FullName == TestMethodEndFullName);
			typeVisitedMethodReference = dependencyMonitorType.Methods.Single(x => x.FullName == TypeMethodFullName);

			testAssemblies = tests.Select(x => x.AssemblyPath).Distinct().ToList();
			var parsingResult = await assembliesAdapter.Parse(toInstrument.AbsoluteSolutionPath, token);
			assemblies = parsingResult.Select(x => x.AbsolutePath).ToList();
			assemblyNames = assemblies.Select(Path.GetFileName).ToList();
			msTestTestcases = tests;
			testNamesToExecutionIds = tests.Select(x => new Tuple<string, int>(x.Id, tests.IndexOf(x))).ToList();

			loggingHelper.ReportNeededTime(() => InstrumentProgramAssemblies(token), "Instrumenting Program Assemblies");
			loggingHelper.ReportNeededTime(() => InstrumentTestAssemblies(token), "Instrumenting Test Assemblies");
		}

		public CorrespondenceLinks GetCorrespondenceLinks()
		{
			var coverageData = new HashSet<Tuple<string, string>>();

			foreach (var file in Directory.GetFiles(DependenciesFolder))
			{
				using (FileStream stream = File.OpenRead(file))
				{
					using (StreamReader streamReader = new StreamReader(stream))
					{
						using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
						{
							var serializer = JsonSerializer.Create(new JsonSerializerSettings {Formatting = Formatting.Indented});
							var dependencies = serializer.Deserialize<HashSet<string>>(jsonReader);

							int testExecutionId = Convert.ToInt32(Path.GetFileNameWithoutExtension(file));
							string testId = testNamesToExecutionIds.Single(x => x.Item2 == testExecutionId).Item1;


							foreach (var dependency in dependencies)
							{
								coverageData.Add(new Tuple<string, string>(testId, dependency));
							}
						}
					}
				}
			}

			return new CorrespondenceLinks(coverageData);
		}

		#region Instrumenting TestAssemblies

		private void InstrumentTestAssemblies(CancellationToken cancellationToken)
		{
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			}; 

			foreach (var testAssembly in testAssemblies)
			{
				cancellationToken.ThrowIfCancellationRequested();
				InstrumentTestAssembly(testAssembly, parallelOptions.CancellationToken);
			}
			/*Parallel.ForEach(testAssemblies, parallelOptions, testAssembly =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				InstrumentTestAssembly(testAssembly, parallelOptions.CancellationToken);
			}); */

			foreach (var testAssembly in testAssemblies)
			{
				UpdateAssemblyCopies(testAssembly);
			}
		}

		private void InstrumentTestAssembly(string testAssembly, CancellationToken cancellationToken)
		{
			if (!File.Exists(testAssembly))
			{
				loggingHelper.WriteMessage($"Warning: {testAssembly} does not exist!");
				return;
			}

			using (var moduleDefinition = LoadModule(testAssembly))
			{
				if (AlreadInstrumented(moduleDefinition))
				{
					return;
				}

				ModuleDefinition msTestModule = null;

				foreach (var type in moduleDefinition.GetTypes())
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (type.Name == MonoModuleTyp)
					{
						continue;
					}

					InstrumentType(type);

					if (type.HasMethods)
					{
						//Roslyn uses '+' for nested classes, Mono '/'
						if (!type.Methods.Any(x => msTestTestcases.Any(y => y.Id == $"{type.FullName.Replace('/', '+')}.{x.Name}")))
						{
							continue;
						}

						if (msTestModule == null)
						{
							msTestModule = GetMsTestModule(moduleDefinition);
						}

						var testInitMethod = InsertMethodWithAttributeIfNotExists(type, TestInitName, msTestModule, MSTestConstants.MsTestTestInitializeAttributeFullName);
						var testCleanupMethod = InsertMethodWithAttributeIfNotExists(type, TestCleanupName, msTestModule, MSTestConstants.MsTestTestCleanupAttributeFullName);

						InsertCallToDependencyMonitorBeginning(testInitMethod, testMethodStartedReference);
						InsertCallToDependencyMonitorEnd(testCleanupMethod, testMethodEndReference);

						foreach (var method in type.Methods)
						{
							cancellationToken.ThrowIfCancellationRequested();
							var id = $"{type.FullName}.{method.Name}";
							if (msTestTestcases.Any(x => x.Id == id))
							{
								string methodArgument = testNamesToExecutionIds.Single(x => x.Item1 == id).Item2.ToString();
								InsertCallToDependencyMonitorBeginning(method, testMethodNameReference, methodArgument, type.FullName);
							}
						}
					}
				}

				UpdateModule(moduleDefinition);
			}
		}

		private MethodDefinition InsertMethodWithAttributeIfNotExists(TypeDefinition type, string methodName, ModuleDefinition attributeModule, string attributeName)
		{
			if (type == null)
			{
				return null;
			}

			var existingMethod = type.Methods.SingleOrDefault(x => x.HasCustomAttributes && x.CustomAttributes.Any(y => y.AttributeType.FullName == attributeName));
			if (existingMethod != null)
			{
				return existingMethod;
			}

			// create new method
			MethodDefinition newMethod = new MethodDefinition(methodName, MethodAttributes.Public, type.Module.TypeSystem.Void);
			// add new method to desired type
			type.Methods.Add(newMethod);
			ILProcessor il = newMethod.Body.GetILProcessor();
			// insert return instruction that has to be at the end of the method
			il.Body.Instructions.Insert(0, il.Create(OpCodes.Ret));

			AddCustomAttribute(newMethod, attributeModule, attributeName);

			return newMethod;
		}

		private ModuleDefinition GetMsTestModule(ModuleDefinition module)
		{
			return GetReferencedModule(module, MSTestConstants.MsTestAssemblyName, MSTestConstants.MsTestModuleName);
		}

		private ModuleDefinition GetReferencedModule(ModuleDefinition module, string assemblyName, string moduleName)
		{
			var assembly = GetReferencedAssembly(module, assemblyName);
			return assembly?.Modules.FirstOrDefault(m => m.Name == moduleName);
		}

		private AssemblyDefinition GetReferencedAssembly(ModuleDefinition module, string assemblyName, HashSet<string> visited = null)
		{
			visited = visited ?? new HashSet<string>();
			if (!visited.Add(module.FileName))
			{
				return null;
			}

			var referencedAssemblies = GetReferencedAssemblies(module);
			if (referencedAssemblies == null)
			{
				return null;
			}

			var result = referencedAssemblies.FirstOrDefault(a => a.Name.Name == assemblyName);
			if (result != null)
			{
				foreach (var referencedAssembly in referencedAssemblies)
				{
					if (referencedAssembly != result)
					{
						referencedAssembly.Dispose();
					}
				}
				return result;
			}

			foreach (var refAsm in referencedAssemblies)
			{
				result = GetReferencedAssembly(refAsm.MainModule, assemblyName, visited);
				if (result != null)
				{
					foreach (var referencedAssembly in referencedAssemblies)
					{
						if (referencedAssembly != result)
						{
							referencedAssembly.Dispose();
						}
					}
					return result;
				}
			}

			return null;
		}

		private IList<AssemblyDefinition> GetReferencedAssemblies(ModuleDefinition module)
		{
			if (module == null)
			{
				return null;
			}

			List<AssemblyDefinition> referencedAssemblies = new List<AssemblyDefinition>();
			foreach (var assemblyNameReference in module.AssemblyReferences)
			{
				var assemblyResolver = new DefaultAssemblyResolver();
				assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(module.FileName));
				referencedAssemblies.Add(assemblyResolver.Resolve(assemblyNameReference));
			}
			return referencedAssemblies;
		}

		private void AddCustomAttribute(MethodDefinition method, ModuleDefinition attributeModule, string attributeName)
		{
			TypeDefinition attributeType = attributeModule.GetType(attributeName);

			var constructor = GetDefaultConstructor(attributeType);
			var constructorMethodRef = method.Module.ImportReference(constructor);
			method.CustomAttributes.Add(new CustomAttribute(constructorMethodRef));
		}

		private MethodDefinition GetDefaultConstructor(TypeDefinition type)
		{
			IEnumerable<MethodDefinition> constructors = type.GetConstructors();
			foreach (MethodDefinition constructor in constructors)
			{
				if (!constructor.HasCustomAttributes)
				{
					return constructor;
				}
			}
			return null;
		}

		#endregion

		#region Instrumenting ProgramAssemblies

		private void InstrumentProgramAssemblies(CancellationToken cancellationToken)
		{
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			foreach (var assembly in assemblies.Except(testAssemblies))
			{
				cancellationToken.ThrowIfCancellationRequested();
				InstrumentProgramAssembly(assembly, parallelOptions.CancellationToken);
			}
			/*Parallel.ForEach(assemblies.Except(testAssemblies), parallelOptions, assembly =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				InstrumentProgramAssembly(assembly, parallelOptions.CancellationToken);
			});*/
		}

		private void InstrumentProgramAssembly(string assembly, CancellationToken cancellationToken)
		{
			if (!File.Exists(assembly))
			{
				loggingHelper.WriteMessage($"Warning: {assembly} does not exist!");
				return;
			}

			using (var moduleDefinition = LoadModule(assembly))
			{
				if (AlreadInstrumented(moduleDefinition))
				{
					return;
				}

				foreach (var type in moduleDefinition.GetTypes())
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (type.Name == MonoModuleTyp)
					{
						continue;
					}

					InstrumentType(type);
				}

				UpdateModule(moduleDefinition);
			}
		}

		#endregion

		#region Assemblies Handling

		private ModuleDefinition LoadModule(string modulePath)
		{
			var resolver = new DefaultAssemblyResolver();

			var moduleDirectory = Path.GetDirectoryName(modulePath);
			//Current Directory and all sub directories
			AddSearchDirectory(moduleDirectory, resolver);

			if (moduleDirectory != null)
			{
				foreach (var reference in settingsProvider.AdditionalReferences)
				{
					string fullPath = reference;

					if (!Path.IsPathRooted(reference))
					{
						fullPath = Path.Combine(moduleDirectory, reference);
					}
					
					if (Directory.Exists(fullPath))
					{
						AddSearchDirectory(fullPath, resolver);
					}
				}
			}

			return ModuleDefinition.ReadModule(modulePath, new ReaderParameters { AssemblyResolver = resolver });
		}

		private void AddSearchDirectory(string directory, DefaultAssemblyResolver resolver)
		{
			resolver.AddSearchDirectory(directory);
			foreach (var subDirectory in Directory.EnumerateDirectories(directory))
			{
				AddSearchDirectory(subDirectory, resolver);
			}
		}

		/// <summary>
		/// Assume that an assembly is either instrumented completly or not at all
		/// </summary>
		/// <param name="moduleDefinition"></param>
		/// <returns></returns>
		private bool AlreadInstrumented(ModuleDefinition moduleDefinition)
		{
			if (moduleDefinition.HasAssemblyReferences)
			{
				return moduleDefinition.AssemblyReferences.Any(x => x.Name == MonitorAssemblyName);
			}

			return false;
		}

		private void UpdateModule(ModuleDefinition moduleDefinition)
		{
			var fileName = moduleDefinition.FileName;

			using (var writer = File.Create(fileName + "_new"))
			{
				moduleDefinition.Write(writer);
			}

			moduleDefinition.Dispose();

			if (File.Exists(fileName + "_old"))
			{
				File.Delete(fileName + "_old");
			}

			File.Move(fileName, fileName + "_old");

			try
			{
				File.Move(fileName + "_new", fileName);
			}
			catch (Exception)
			{
				throw new ArgumentException($"The assembly '{fileName}' is locked!");
			}
		}

		private void UpdateAssemblyCopies(string testAssembly)
		{
			var directory = Path.GetDirectoryName(testAssembly);
			if (directory != null)
			{
				foreach (var file in Directory.GetFiles(directory))
				{
					if (file == testAssembly)
					{
						continue;
					}

					var fileName = Path.GetFileName(file);

					if (assemblyNames.Any(x => x.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)))
					{
						var fullPath = assemblies.ElementAt(assemblyNames.IndexOf(fileName));
						try
						{
							File.Copy(fullPath, file, true);
						}
						catch (Exception)
						{
							loggingHelper.WriteMessage($"Warning: Unable to overwrite file {file}");
						}
					}
				}
			}
		}
		private void RecoverOldAssemblies()
		{
			foreach (var assembly in assemblies)
			{
				if (File.Exists(assembly + "_old"))
				{
					if (File.Exists(assembly))
					{
						File.Delete(assembly);
					}

					File.Move(assembly + "_old", assembly);
				}
			}

			foreach (var testAssembly in testAssemblies)
			{
				UpdateAssemblyCopies(testAssembly);
			}
		}

		#endregion

		#region Instrumenting Type for Dependencies

		private void InstrumentType(TypeDefinition typeDefinition)
		{
			foreach (MethodDefinition method in typeDefinition.Methods)
			{
				if (!method.HasBody)
				{
					continue;
				}

				List<Tuple<Instruction, TypeReference>> instrumentationPoints = GetInstrumentationPoints(method);

				InstrumentAtPoints(method, instrumentationPoints);
			}
		}

		private void InstrumentAtPoints(MethodDefinition method, List<Tuple<Instruction, TypeReference>> instrumentationPoints)
		{
			if (instrumentationPoints.Count == 0)
			{
				return;
			}

			method.Body.SimplifyMacros();
			foreach (var pair in instrumentationPoints)
			{
				Instruction instruction = pair.Item1;
				TypeReference dependency = pair.Item2;
				string dependencyIdentifier = GetTypeReferenceIdentifier(dependency);

				if (dependencyIdentifier == null)
					continue;

				if (instruction == null)
				{
					InsertCallToDependencyMonitorBeginning(method, typeVisitedMethodReference, dependencyIdentifier);
				}
				else
				{
					InsertCallToDependencyMonitor(method, instruction, typeVisitedMethodReference, dependencyIdentifier);
				}
			}
			method.Body.OptimizeMacros();
		}

		private string GetTypeReferenceIdentifier(TypeReference dependency)
		{
			return dependency.FullName;
		}

		/// <summary>
		/// Returns a list of Instruction, Type pairs 
		/// that represent instrumentation point and coresponding type respectively.
		/// If instruction is null than whole method is a dependency
		/// </summary>
		private List<Tuple<Instruction, TypeReference>> GetInstrumentationPoints(MethodDefinition method)
		{
			List<Tuple<Instruction, TypeReference>> instrumentationPoints = new List<Tuple<Instruction, TypeReference>>();

			if (method.IsConstructor)
			{
				instrumentationPoints.Add(new Tuple<Instruction, TypeReference>(null, method.DeclaringType));
			}

			foreach (var instruction in method.Body.Instructions)
			{
				if (IsStaticFieldAccess(instruction))
				{
					TypeReference type = GetStaticFieldAccessTarget(instruction);
					if (WithinModuleUnderInstrumentation(type))
					{
						instrumentationPoints.Add(new Tuple<Instruction, TypeReference>(instruction, type));
					}
				}
				else if (IsStaticMethodCall(instruction))
				{
					TypeReference type = GetStaticMethodCallTarget(instruction);
					if (WithinModuleUnderInstrumentation(type))
					{
						instrumentationPoints.Add(new Tuple<Instruction, TypeReference>(instruction, type));
					}
				}
			}

			return instrumentationPoints;
		}

		private TypeReference GetStaticMethodCallTarget(Instruction staticMethodCallInstruction)
		{
			MethodReference callee = staticMethodCallInstruction.Operand as MethodReference;
			// type containing method called used callInstruction
			TypeReference declaringType = callee?.DeclaringType;
			return declaringType?.GetElementType();
		}

		private bool WithinModuleUnderInstrumentation(TypeReference type)
		{
			return assemblies.Select(Path.GetFileName).Contains(type.Scope.Name);
		}

		private bool IsStaticMethodCall(Instruction instruction)
		{
			bool isCall = instruction.OpCode == OpCodes.Call;
			if (!isCall)
			{
				return false;
			}
			//callee not this
			MethodReference callee = instruction.Operand as MethodReference;
			return !callee?.HasThis ?? false;
		}

		private TypeReference GetStaticFieldAccessTarget(Instruction staticFieldAccessInstruction)
		{
			FieldReference operand = staticFieldAccessInstruction.Operand as FieldReference;
			TypeReference declaringType = operand?.DeclaringType;

			return declaringType?.GetElementType();
		}

		private bool IsStaticFieldAccess(Instruction instruction)
		{
			return instruction.OpCode == OpCodes.Ldsfld ||
			       instruction.OpCode == OpCodes.Ldsflda ||
			       instruction.OpCode == OpCodes.Stsfld;
		}

		#endregion

		#region Instrumenting Instructions

		private void InsertCallToDependencyMonitorBeginning(MethodDefinition methodToInstrument, MethodReference dependencyMonitorMethodToCall,
			params string[] methodArguments)
		{
			if (methodToInstrument.HasBody && methodToInstrument.Body.Instructions.Any())
			{
				Instruction firstInstruction = methodToInstrument.Body.Instructions.First();
				InsertCallToDependencyMonitor(methodToInstrument, firstInstruction, dependencyMonitorMethodToCall, methodArguments);
			}
		}

		private void InsertCallToDependencyMonitorEnd(MethodDefinition methodToInstrument, MethodReference dependencyMonitorMethodToCall,
			params string[] methodArguments)
		{
			if (methodToInstrument.HasBody && methodToInstrument.Body.Instructions.Any())
			{
				Instruction lastInstruction = methodToInstrument.Body.Instructions.Last();
				InsertCallToDependencyMonitor(methodToInstrument, lastInstruction, dependencyMonitorMethodToCall, methodArguments);
			}
		}

		private void InsertCallToDependencyMonitor(MethodDefinition methodToInstrument, Instruction insertCallBefore,
			MethodReference dependencyMonitorMethodToCall, params string[] methodArguments)
		{
			MethodReference methodToCall = dependencyMonitorMethodToCall;
			if (methodToInstrument.Module.FileName != dependencyMonitorMethodToCall.Module.FileName)
			{
				methodToCall = methodToInstrument.Module.ImportReference(dependencyMonitorMethodToCall);
			}

			ILProcessor il = methodToInstrument.Body.GetILProcessor();
			Instruction callInstruction = il.Create(OpCodes.Call, methodToCall);
			il.InsertBefore(insertCallBefore, callInstruction);

			foreach (var methodArgument in methodArguments)
			{
				Instruction ldStrInstruction = il.Create(OpCodes.Ldstr, methodArgument);
				il.InsertBefore(callInstruction, ldStrInstruction);
			}
		}

		#endregion

		public void Dispose()
		{
			//Cleanup Dependencies Folder
			foreach (var file in Directory.GetFiles(DependenciesFolder))
			{
				File.Delete(file);
			}

			RecoverOldAssemblies();
		}
	}
}