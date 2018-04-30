using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Models.TestExecution;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestExecutorWithInstrumenting : ITestProcessor<MSTestTestcase, MSTestExectionResult>
	{
		public event EventHandler<TestCaseResultEventArgs<MSTestTestcase>> TestResultAvailable;

		private const string MonoModuleTyp = "<Module>";

		private ModuleDefinition dependencyMonitorModule;
		private TypeDefinition dependencyMonitorType;
		private MethodReference testMethodStartedReference;

		private readonly InProcessMSTestTestsExecutor executor;

		public MSTestExecutorWithInstrumenting(InProcessMSTestTestsExecutor executor)
		{
			this.executor = executor;
		}

		public Task<MSTestExectionResult> ProcessTests(IEnumerable<MSTestTestcase> tests, CancellationToken cancellationToken)
		{
			dependencyMonitorModule = ModuleDefinition.ReadModule(Path.GetFullPath("RTSFramework.Concrete.CSharp.DependencyMonitor.dll"));
			dependencyMonitorType = dependencyMonitorModule.Types.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.ClassFullName);
			testMethodStartedReference = dependencyMonitorType.Methods.Single(x => x.FullName == DependencyMonitor.DependencyMonitor.TestMethodStartFullName);

			var msTestTestcases = tests as IList<MSTestTestcase> ?? tests.ToList();
			foreach (var assembly in msTestTestcases.Select(x => x.AssemblyPath).Distinct())
			{
				var moduleDefinition = ModuleDefinition.ReadModule(assembly);
				foreach (var type in moduleDefinition.Types)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (type.Name == MonoModuleTyp)
					{
						continue;
					}

					if (type.HasMethods)
					{
						foreach (var method in type.Methods)
						{
							var id = $"{type.FullName}.{method.Name}";
							if (msTestTestcases.Any(x => x.Id == id))
							{
								InstrumentTestMethod(method);
							}
						}
					}
				}

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
				File.Move(fileName + "_new", fileName);
			}

			executor.TestResultAvailable += TestResultAvailable;
			return executor.ProcessTests(msTestTestcases, cancellationToken);
		}

		private void InstrumentTestMethod(MethodDefinition testMethod)
		{
			var methodArgument = $"{testMethod.DeclaringType.FullName}.{testMethod.Name}";

			InsertCallToDependencyMonitor(testMethod, testMethodStartedReference, methodArgument);
		}

		private void InsertCallToDependencyMonitor(MethodDefinition methodToInstrument, MethodReference dependencyMonitorMethodToCall, string methodArgument)
		{
			if (methodToInstrument.HasBody && methodToInstrument.Body.Instructions.Any())
			{
				Instruction firstInstruction = methodToInstrument.Body.Instructions[0];
				InsertCallToDependencyMonitor(methodToInstrument, firstInstruction, dependencyMonitorMethodToCall, methodArgument);
			}
		}

		private bool AlreadyInstrumented(MethodReference dependencyMonitorMethodToCall, Instruction instr, string methodArgument)
		{
			return instr.OpCode == OpCodes.Ldstr &&
			       instr.Operand is string 
				   && string.Equals((string) instr.Operand, methodArgument, StringComparison.Ordinal)
			       && instr.Next != null 
				   && instr.Next.OpCode == OpCodes.Call 
				   && instr.Next.Operand is MethodReference
				   && string.Equals(((MethodReference)instr.Next.Operand).FullName, dependencyMonitorMethodToCall.FullName);
		}

		private void InsertCallToDependencyMonitor(MethodDefinition methodToInstrument, Instruction insertCallBefore, MethodReference dependencyMonitorMethodToCall, string methodArgument)
		{
			if (AlreadyInstrumented(dependencyMonitorMethodToCall, insertCallBefore, methodArgument))
			{
				return;
			}

			MethodReference methodToCall = dependencyMonitorMethodToCall;
			if (methodToInstrument.Module.FileName != dependencyMonitorMethodToCall.Module.FileName)
			{
				methodToCall = methodToInstrument.Module.ImportReference(dependencyMonitorMethodToCall);
			}

			ILProcessor il = methodToInstrument.Body.GetILProcessor();
			Instruction callInstruction = il.Create(OpCodes.Call, methodToCall);
			il.InsertBefore(insertCallBefore, callInstruction);

			Instruction ldStrInstruction = il.Create(OpCodes.Ldstr, methodArgument);
			il.InsertBefore(callInstruction, ldStrInstruction);
		}

	}
}