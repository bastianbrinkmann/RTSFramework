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
	public class MonoMSTestTestsDiscoverer<TModel> : ITestsDiscoverer<TModel, MSTestTestcase> where TModel : CSharpProgramModel
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;

		public MonoMSTestTestsDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter)
		{
			this.assembliesAdapter = assembliesAdapter;
		}

		public async Task<IEnumerable<MSTestTestcase>> GetTestCasesForModel(TModel model, CancellationToken token)
		{
			var testCases = new List<MSTestTestcase>();

			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

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
							token.ThrowIfCancellationRequested();

							if (method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.TestMethodAttributeName))
							{
								var id = $"{type.FullName}.{method.Name}";

								var testCase = new MSTestTestcase
								{
									Name = method.Name,
									AssemblyPath = modulePath,
									Id = id,
									FullClassName = type.FullName
								};

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
			return ModuleDefinition.ReadModule(moduleFilePath);
		}
	}
}