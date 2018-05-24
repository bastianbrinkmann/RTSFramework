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
using RTSFramework.Contracts.Utilities;

namespace RTSFramework.Concrete.CSharp.MSTest
{
	public class MonoMSTestTestDiscoverer<TModel> : ITestDiscoverer<TModel, MSTestTestcase> where TModel : CSharpProgramModel
	{
		private readonly CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter;
		private readonly ISettingsProvider settingsProvider;

		public MonoMSTestTestDiscoverer(CancelableArtefactAdapter<string, IList<CSharpAssembly>> assembliesAdapter,
			ISettingsProvider settingsProvider)
		{
			this.assembliesAdapter = assembliesAdapter;
			this.settingsProvider = settingsProvider;

		}

		public async Task<ISet<MSTestTestcase>> GetTests(TModel model, Func<MSTestTestcase, bool> filterFunction, CancellationToken token)
		{
			var testCases = new HashSet<MSTestTestcase>();

			var parsingResult = await assembliesAdapter.Parse(model.AbsoluteSolutionPath, token);
			token.ThrowIfCancellationRequested();

			var sources = parsingResult.Select(x => x.AbsolutePath).Where(x => x.EndsWith(settingsProvider.TestAssembliesFilter));

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
									AssociatedClass = type.FullName
								};

								var categoryAttributes =
									method.CustomAttributes.Where(x => x.AttributeType.Name == MSTestConstants.TestCategoryAttributeName);
								foreach (var categoryAttr in categoryAttributes)
								{
									testCase.Categories.Add((string)categoryAttr.ConstructorArguments[0].Value);
								}

								testCase.Ignored = method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.IgnoreAttributeName);

								if (!testCase.Ignored && filterFunction(testCase))
								{
									testCases.Add(testCase);
								}
							}
						}
					}
				}
			}
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