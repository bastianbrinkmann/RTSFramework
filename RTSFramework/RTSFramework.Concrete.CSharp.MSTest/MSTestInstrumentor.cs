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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Newtonsoft.Json;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Utilities;
using RTSFramework.Core.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestInstrumentor<TModel> : ITestInstrumentor<TModel, MSTestTestcase> where TModel : CSharpProgramModel
	{
		private const string MonoModuleTyp = "<Module>";
		private const string DependenciesFolder = @"TestResults\Dependencies";
		private const string MonitorAssemblyName = "RTSFramework.Concrete.CSharp.DependencyMonitor";

		private ModuleDefinition dependencyMonitorModule;
		private TypeDefinition dependencyMonitorType;
		private MethodReference testMethodStartedReference;
		private MethodReference testMethodEndReference;
		private MethodReference typeVisitedMethodReference;

		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly ILoggingHelper loggingHelper;

		public MSTestInstrumentor(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter,
			ILoggingHelper loggingHelper)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.loggingHelper = loggingHelper;
		}

		private List<string> assemblies;
		private List<string> assemblyNames;
		private IList<MSTestTestcase> msTestTestcases;

		public async Task InstrumentModelForTests(TModel toInstrument, IList<MSTestTestcase> tests, CancellationToken token)
		{
			dependencyMonitorModule = ModuleDefinition.ReadModule(Path.GetFullPath($"{MonitorAssemblyName}.dll"));
			dependencyMonitorType = dependencyMonitorModule.Types.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.ClassFullName);
			testMethodStartedReference = dependencyMonitorType.Methods.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.TestMethodStartFullName);
			testMethodEndReference = dependencyMonitorType.Methods.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.TestMethodEndFullName);
			typeVisitedMethodReference = dependencyMonitorType.Methods.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.TypeMethodFullName);

			var testAssemblies = tests.Select(x => x.AssemblyPath).Distinct().ToList();
			var parsingResult = await assembliesAdapter.Parse(toInstrument.AbsoluteSolutionPath, token);
			assemblies = parsingResult.Select(x => x.AbsolutePath).ToList();
			assemblyNames = assemblies.Select(Path.GetFileName).ToList();
			msTestTestcases = tests;

			loggingHelper.ReportNeededTime(() => InstrumentProgramAssemblies(token, testAssemblies), "Instrumenting Program Assemblies");
			loggingHelper.ReportNeededTime(() => InstrumentTestAssemblies(token, testAssemblies), "Instrumenting Test Assemblies");
		}

		public CoverageData GetCoverageData()
		{
			var coverageData = new HashSet<CoverageDataEntry>();

			foreach (var file in Directory.GetFiles(DependenciesFolder))
			{
				using (FileStream stream = File.OpenRead(file))
				{
					using (StreamReader streamReader = new StreamReader(stream))
					{
						using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
						{
							var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
							var dependencies = serializer.Deserialize<HashSet<string>>(jsonReader);

							var testId = Path.GetFileNameWithoutExtension(file);

							foreach (var dependency in dependencies)
							{
								coverageData.Add(new CoverageDataEntry
								{
									TestCaseId = testId,
									ClassName = dependency
								});
							}
						}
					}
				}

				File.Delete(file);
			}

			return new CoverageData(coverageData);
		}

		#region Instrumenting TestAssemblies

		private void InstrumentTestAssemblies(CancellationToken cancellationToken, List<string> testAssemblies)
		{
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			Parallel.ForEach(testAssemblies, parallelOptions, testAssembly =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				InstrumentTestAssembly(testAssembly, parallelOptions.CancellationToken);
			});
		}

		private void InstrumentTestAssembly(string testAssembly, CancellationToken cancellationToken)
		{
			if (!File.Exists(testAssembly))
			{
				loggingHelper.WriteMessage($"Warning: {testAssembly} does not exist!");
				return;
			}

			var moduleDefinition = ModuleDefinition.ReadModule(testAssembly);
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

				if (type.HasMethods)
				{
					foreach (var method in type.Methods)
					{
						cancellationToken.ThrowIfCancellationRequested();
						var id = $"{type.FullName}.{method.Name}";
						if (msTestTestcases.Any(x => x.Id == id))
						{
							InstrumentTestMethod(method);
						}
					}
				}
			}

			UpdateModule(moduleDefinition);
			UpdateAssemblyCopies(testAssembly);
		}

		#endregion

		#region Instrumenting ProgramAssemblies

		private void InstrumentProgramAssemblies(CancellationToken cancellationToken, List<string> testAssemblies)
		{
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			Parallel.ForEach(assemblies.Except(testAssemblies), parallelOptions, assembly =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				InstrumentProgramAssembly(assembly, parallelOptions.CancellationToken);
			});
		}

		private void InstrumentProgramAssembly(string assembly, CancellationToken cancellationToken)
		{
			if (!File.Exists(assembly))
			{
				loggingHelper.WriteMessage($"Warning: {assembly} does not exist!");
				return;
			}

			var moduleDefinition = ModuleDefinition.ReadModule(assembly);
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

		#endregion

		private bool AlreadInstrumented(ModuleDefinition moduleDefinition)
		{
			if (moduleDefinition.HasAssemblyReferences)
			{
				return moduleDefinition.AssemblyReferences.Any(x => x.Name == MonitorAssemblyName);
			}

			return false;
		}

		#region Assemblies Handling
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
						File.Copy(fullPath, file, true);
					}
				}
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
				if (instruction == null)
				{
					InsertCallToDependencyMonitorBeginning(method, typeVisitedMethodReference, dependency.FullName);
				}
				else
				{
					InsertCallToDependencyMonitor(method, instruction, typeVisitedMethodReference, dependency.FullName);
				}
			}
			method.Body.OptimizeMacros();
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


		private void InstrumentTestMethod(MethodDefinition testMethod)
		{
			var methodArgument = $"{testMethod.DeclaringType.FullName}.{testMethod.Name}";

			InsertCallToDependencyMonitorBeginning(testMethod, testMethodStartedReference, methodArgument);
			InsertCallToDependencyMonitorEnd(testMethod, testMethodEndReference);
		}

		#region Instrumenting Instructions

		private void InsertCallToDependencyMonitorBeginning(MethodDefinition methodToInstrument, MethodReference dependencyMonitorMethodToCall, string methodArgument)
		{
			if (methodToInstrument.HasBody && methodToInstrument.Body.Instructions.Any())
			{
				Instruction firstInstruction = methodToInstrument.Body.Instructions.First();
				InsertCallToDependencyMonitor(methodToInstrument, firstInstruction, dependencyMonitorMethodToCall, methodArgument);
			}
		}

		private void InsertCallToDependencyMonitorEnd(MethodDefinition methodToInstrument, MethodReference dependencyMonitorMethodToCall)
		{
			if (methodToInstrument.HasBody && methodToInstrument.Body.Instructions.Any())
			{
				Instruction lastInstruction = methodToInstrument.Body.Instructions.Last();
				InsertCallToDependencyMonitor(methodToInstrument, lastInstruction, dependencyMonitorMethodToCall);
			}
		}

		private void InsertCallToDependencyMonitor(MethodDefinition methodToInstrument, Instruction insertCallBefore, MethodReference dependencyMonitorMethodToCall, string methodArgument = null)
		{
			MethodReference methodToCall = dependencyMonitorMethodToCall;
			if (methodToInstrument.Module.FileName != dependencyMonitorMethodToCall.Module.FileName)
			{
				methodToCall = methodToInstrument.Module.ImportReference(dependencyMonitorMethodToCall);
			}

			ILProcessor il = methodToInstrument.Body.GetILProcessor();
			Instruction callInstruction = il.Create(OpCodes.Call, methodToCall);
			il.InsertBefore(insertCallBefore, callInstruction);

			if (methodArgument != null)
			{
				Instruction ldStrInstruction = il.Create(OpCodes.Ldstr, methodArgument);
				il.InsertBefore(callInstruction, ldStrInstruction);
			}
		}

		#endregion

	}
}