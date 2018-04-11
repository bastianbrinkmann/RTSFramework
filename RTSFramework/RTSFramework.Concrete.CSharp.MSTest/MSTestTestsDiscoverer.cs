using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Adapter;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MSTestTestsDiscoverer<TModel> : ITestsDiscoverer<TModel, MSTestTestcase> where TModel : CSharpProgramModel
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;

		public MSTestTestsDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter)
		{
			this.assembliesAdapter = assembliesAdapter;
		}

		//TODO: Think about discovery of tests based on source code (using ASTs)
		//Advantage: can identify impacted tests even if code does not compile
		//Disadvantage: maybe requires lot more time?
		public async Task<IEnumerable<MSTestTestcase>> GetTestCasesForModel(TModel model, CancellationToken token = default(CancellationToken))
		{
			var testCases = new List<MSTestTestcase>();

			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			if (token.IsCancellationRequested)
			{
				return testCases;
			}

			//TODO Filtering of Test dlls?
			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith("Test.dll"));

			foreach (var modulePath in sources)
			{
				ModuleDefinition module = GetMonoModuleDefinition(modulePath);
				foreach (TypeDefinition type in module.Types)
				{
					if (type.HasMethods)
					{
						foreach (MethodDefinition method in type.Methods)
						{
							if (method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.TestMethodAttributeName))
							{
								var testCase = new MSTestTestcase(modulePath, method.Name, type.FullName);

								var categoryAttributes =
									method.CustomAttributes.Where(x => x.AttributeType.Name == MSTestConstants.TestCategoryAttributeName);
								foreach (var categoryAttr in categoryAttributes)
								{
									testCase.Categories.Add((string)categoryAttr.ConstructorArguments[0].Value);
								}

								testCase.Ignored = method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.IgnoreAttributeName);

								testCases.Add(testCase);
							}
						}
					}
				}
			}
			//TODO: Filtering of TestCases
			//allTests = allTests.Where(x => x.Categories.Any(y => y == "Default"));
			return testCases;
		}

		private ModuleDefinition GetMonoModuleDefinition(string moduleFilePath)
		{
			FileInfo fileInfo = new FileInfo(moduleFilePath);
			if (!fileInfo.Exists)
			{
				throw new ArgumentException("Invalid path to a module: " + moduleFilePath);
			}
			ModuleDefinition module;

			ReaderParameters parameters = new ReaderParameters { ReadSymbols = true };
			try
			{
				module = ModuleDefinition.ReadModule(moduleFilePath, parameters);
			}
			catch (Exception)
			{
				module = ModuleDefinition.ReadModule(moduleFilePath);
			}

			return module;
		}


	}
}