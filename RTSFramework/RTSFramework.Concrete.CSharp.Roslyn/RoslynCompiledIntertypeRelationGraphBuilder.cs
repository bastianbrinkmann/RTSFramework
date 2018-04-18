using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
	public class RoslynCompiledIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureProvider<IntertypeRelationGraph, TCSharpModel> where TCSharpModel : CSharpProgramModel
	{
		private List<Compilation> compilations;

		public async Task<IntertypeRelationGraph> GetDataStructureForProgram(TCSharpModel sourceModel, CancellationToken token)
		{
			var graph = new IntertypeRelationGraph();
			var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			// ReSharper disable once RedundantEmptyObjectOrCollectionInitializer
			{
				//{ "Configuration", "Debug" },//{ "Configuration", "net_3_5_Debug_ReadOnly" },
				//{ "Platform", "Any CPU" }
			});

			var solution = await workspace.OpenSolutionAsync(sourceModel.AbsoluteSolutionPath, token);

			compilations = new List<Compilation>();

			var typeSymbols = new List<INamedTypeSymbol>();

			//Collect all types
			foreach (var project in solution.Projects)
			{
				token.ThrowIfCancellationRequested();

				var compilation = await project.GetCompilationAsync(token);

				compilations.Add(compilation);

				foreach (var namespaceSymbol in compilation.Assembly.GlobalNamespace.GetNamespaceMembers())
				{
					AddNodeIfNotExists(namespaceSymbol, graph, typeSymbols);
				}
			}

			//Build edges
			foreach (INamedTypeSymbol type in typeSymbols)
			{
				if (type.BaseType != null)
				{
					TrackAverageTimes("BaseType", () =>
					{
						AddInheritanceEdgeIfBothExist(type, type.BaseType, graph);
					});
				}

				TrackAverageTimes("Interfaces", () =>
				{
					foreach (var typeInterface in type.Interfaces)
					{
						AddInheritanceEdgeIfBothExist(type, typeInterface, graph);
					}
				});

				TrackAverageTimes("Attributes", () =>
				{
					ProcessAttributes(type, type, graph);
				});

				foreach (var symbol in type.GetMembers())
				{
					TrackAverageTimes("MemberAttributesTypeParams", () =>
					{
						ProcessAttributes(type, symbol, graph);

						foreach (var typeParameter in type.TypeParameters)
						{
							foreach (var constraint in typeParameter.ConstraintTypes)
							{
								AddUseEdgeIfBothExist(type, constraint as INamedTypeSymbol, graph);
							}
						}
					});

					var method = symbol as IMethodSymbol;
					if (method != null)
					{
						TrackAverageTimes("Method", () =>
						{
							ProcessMethodSymbol(type, method, graph);
						});
					}

					var property = symbol as IPropertySymbol;
					if (property != null)
					{
						TrackAverageTimes("Property", () =>
						{
							ProcessPropertySymbol(type, property, graph);
						});
					}

					var field = symbol as IFieldSymbol;
					if (field != null)
					{
						TrackAverageTimes("Field", () =>
						{
							ProcessFieldSymbol(type, field, graph);
						});
					}

					var eventSymbol = symbol as IEventSymbol;
					if (eventSymbol != null)
					{
						TrackAverageTimes("Event", () =>
						{
							ProcessEventSymbol(type, eventSymbol, graph);
						});
					}
				}
			}

			PrintTrackedTimes();

			return graph;
		}

		#region TimeTracking

		private void PrintTrackedTimes()
		{
			foreach (var entry in averageTimesDictionary.Where(x => x.Key.StartsWith("AverageTime_")).OrderByDescending(x => x.Value))
			{
				var name = entry.Key.Substring(12, entry.Key.Length - 12);
				var averageTime = averageTimesDictionary[AverageTimeKey(name)];
				var executions = averageTimesDictionary[NumberOfExecutionsKey(name)];

				var averageTimeString = "" + averageTime;
				var executionsString = "" + executions;

				Debug.WriteLine($"{name.PadRight(30)}: {averageTimeString.PadRight(25)} * {executionsString.PadRight(10)} = {averageTime * executions}");
			}
		}

		private Dictionary<string, double> averageTimesDictionary = new Dictionary<string, double>();

		private string AverageTimeKey(string name)
		{
			return "AverageTime_" + name;
		}

		private string NumberOfExecutionsKey(string name)
		{
			return "NumberOfExecutions_" + name;
		}

		private void TrackAverageTimes(string name, Action action)
		{
			if (!averageTimesDictionary.ContainsKey(AverageTimeKey(name)))
			{
				averageTimesDictionary.Add(AverageTimeKey(name), 0);
				averageTimesDictionary.Add(NumberOfExecutionsKey(name), 0);
			}

			var stopWatch = new Stopwatch();
			stopWatch.Start();
			action();
			stopWatch.Stop();

			double averageTime = averageTimesDictionary[AverageTimeKey(name)];
			double numberOfExecutions = averageTimesDictionary[NumberOfExecutionsKey(name)];
			double totalTime = averageTime * numberOfExecutions;

			totalTime += stopWatch.Elapsed.TotalSeconds;
			numberOfExecutions += 1;

			averageTimesDictionary[AverageTimeKey(name)] = totalTime / numberOfExecutions;
			averageTimesDictionary[NumberOfExecutionsKey(name)] = numberOfExecutions;
		}

		#endregion

		private void ProcessEventSymbol(INamedTypeSymbol type, IEventSymbol eventSymbol, IntertypeRelationGraph graph)
		{
			ProcessMethodSymbol(type, eventSymbol.AddMethod, graph);
			ProcessMethodSymbol(type, eventSymbol.RemoveMethod, graph);
			ProcessMethodSymbol(type, eventSymbol.RaiseMethod, graph);
			AddUseEdgeIfBothExist(type, eventSymbol.Type as INamedTypeSymbol, graph);
		}

		private void ProcessFieldSymbol(INamedTypeSymbol type, IFieldSymbol field, IntertypeRelationGraph graph)
		{
			AddUseEdgeIfBothExist(type, field.Type as INamedTypeSymbol, graph);
		}

		private void ProcessAttributes(INamedTypeSymbol type, ISymbol symbol, IntertypeRelationGraph graph)
		{
			foreach (var attribute in symbol.GetAttributes())
			{
				AddUseEdgeIfBothExist(type, attribute.AttributeClass, graph);
			}
		}

		private void ProcessPropertySymbol(INamedTypeSymbol type, IPropertySymbol property, IntertypeRelationGraph graph)
		{
			AddUseEdgeIfBothExist(type, property.Type as INamedTypeSymbol, graph);

			ProcessMethodSymbol(type, property.SetMethod, graph);
			ProcessMethodSymbol(type, property.GetMethod, graph);
		}

		private void ProcessMethodSymbol(INamedTypeSymbol type, IMethodSymbol method, IntertypeRelationGraph graph)
		{
			if (method == null) //Get or Set Method e.g. are not necessarily implemented
			{
				return;
			}

			foreach (var methodParameter in method.Parameters)
			{
				AddUseEdgeIfBothExist(type, methodParameter.Type as INamedTypeSymbol, graph);
			}

			AddUseEdgeIfBothExist(type, method.ReturnType as INamedTypeSymbol, graph);

			foreach (var typeParameter in method.TypeParameters)
			{
				foreach (var constraint in typeParameter.ConstraintTypes)
				{
					AddUseEdgeIfBothExist(type, constraint as INamedTypeSymbol, graph);
				}
			}

			TrackAverageTimes("MethodBody", () =>
			{
				foreach (var methodDeclaringSyntaxReference in method.DeclaringSyntaxReferences)
				{
					var methodSyntax = methodDeclaringSyntaxReference.GetSyntax();
					var compilation = compilations.First(x => Equals(x.Assembly, method.ContainingAssembly));

					var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

					var accessorDec = methodSyntax as AccessorDeclarationSyntax; //Properties
					if (accessorDec?.Body != null)
					{
						ProcessOperations(semanticModel, type, graph, accessorDec.Body);
						continue;
					}

					var arrowExpr = methodSyntax as ArrowExpressionClauseSyntax; //Properties with Arrow
					if (arrowExpr?.Expression != null)
					{
						ProcessOperations(semanticModel, type, graph, arrowExpr.Expression);
						continue;
					}

					var methodDec = methodSyntax as BaseMethodDeclarationSyntax; //Methods, Constructors
					if (methodDec?.Body != null)
					{
						ProcessOperations(semanticModel, type, graph, methodDec.Body);
					}
				}
			});
		}

		private void ProcessOperations(SemanticModel semanticModel, INamedTypeSymbol type, IntertypeRelationGraph graph, SyntaxNode node)
		{
			IOperation operation = semanticModel.GetOperation(node);

			TrackAverageTimes("Operation", () =>
			{
				ProcessOperation(type, operation, graph);
			});
		}

		private void ProcessOperation(INamedTypeSymbol type, IOperation operation, IntertypeRelationGraph graph)
		{
			foreach (var child in operation.Children)
			{
				TrackAverageTimes("Operation", () =>
				{
					ProcessOperation(type, child, graph);
				});
			}

			//AddUseEdgeIfBothExist(type, operation.Type as INamedTypeSymbol, graph);

			switch (operation.Kind)
			{
				case OperationKind.DynamicMemberReference:
					var dynmemberRef = (IDynamicMemberReferenceOperation)operation;
					AddUseEdgeIfBothExist(type, dynmemberRef.ContainingType as INamedTypeSymbol, graph);
					foreach (var typeArg in dynmemberRef.TypeArguments)
					{
						AddUseEdgeIfBothExist(type, typeArg as INamedTypeSymbol, graph);
					}
					break;
				case OperationKind.EventReference:
				case OperationKind.FieldReference:
				case OperationKind.MethodReference:
				case OperationKind.PropertyReference:
					var memberReference = (IMemberReferenceOperation)operation;
					AddUseEdgeIfBothExist(type, memberReference.Member.ContainingType, graph);
					AddUseEdgeIfBothExist(type, memberReference.Member.ContainingType, graph);
					break;
				case OperationKind.TypeOf:
					var typeOfOp = (ITypeOfOperation)operation;
					AddUseEdgeIfBothExist(type, typeOfOp.TypeOperand as INamedTypeSymbol, graph);
					break;
				case OperationKind.ObjectCreation: //TODO: Method Rerference also?
					var objectCreation = (IObjectCreationOperation)operation;
					AddUseEdgeIfBothExist(type, objectCreation.Constructor?.ContainingType, graph);
					break;
				case OperationKind.IsType:
					var isType = (IIsTypeOperation)operation;
					AddUseEdgeIfBothExist(type, isType.TypeOperand as INamedTypeSymbol, graph);
					break;
				case OperationKind.Tuple:
					var tuple = (ITupleOperation)operation;
					AddUseEdgeIfBothExist(type, tuple.NaturalType as INamedTypeSymbol, graph);
					break;
				case OperationKind.SizeOf:
					var sizeOf = (ISizeOfOperation)operation;
					AddUseEdgeIfBothExist(type, sizeOf.TypeOperand as INamedTypeSymbol, graph);
					break;
				case OperationKind.FieldInitializer: //TODO Init without reference?
					var fieldInit = (IFieldInitializerOperation)operation; 
					foreach (var initializedField in fieldInit.InitializedFields)
					{
						AddUseEdgeIfBothExist(type, initializedField.Type as INamedTypeSymbol, graph);
					}
					break;
				case OperationKind.PropertyInitializer:
					var propertyInit = (IPropertyInitializerOperation) operation;
					foreach (var initProperty in propertyInit.InitializedProperties)
					{
						AddUseEdgeIfBothExist(type, initProperty.Type as INamedTypeSymbol, graph);
					}
					break;
				case OperationKind.ParameterInitializer:
					var paramInit = (IParameterInitializerOperation) operation;
					AddUseEdgeIfBothExist(type, paramInit.Parameter.ContainingType, graph);
					break;
				case OperationKind.VariableDeclarator:
					var variableDeclarator = (IVariableDeclaratorOperation) operation;
					AddUseEdgeIfBothExist(type, variableDeclarator.Symbol.Type as INamedTypeSymbol, graph);
					break;
				case OperationKind.Argument:
					var argumentOperation = (IArgumentOperation) operation;
					AddUseEdgeIfBothExist(type, argumentOperation.Parameter.Type as INamedTypeSymbol, graph);
					break;
				case OperationKind.CaseClause:
					var catchClause = (ICatchClauseOperation) operation;
					AddUseEdgeIfBothExist(type, catchClause.ExceptionType as INamedTypeSymbol, graph);
					break;
				case OperationKind.DeclarationPattern:
					var declarationpattern = (IDeclarationPatternOperation) operation;
					AddUseEdgeIfBothExist(type, declarationpattern.DeclaredSymbol as INamedTypeSymbol, graph);
					break;
				case OperationKind.Invocation:
					var incovation = (IInvocationOperation) operation;
					AddUseEdgeIfBothExist(type, incovation.TargetMethod?.ContainingType, graph);
					break;
			}
		}

		#region GraphHelpers

		private void AddNodeIfNotExists(INamespaceOrTypeSymbol namedType, IntertypeRelationGraph graph, List<INamedTypeSymbol> typeSymbols)
		{
			if (namedType == null)
			{
				return;
			}

			if (namedType.IsType)
			{
				typeSymbols.Add((INamedTypeSymbol)namedType);

				var typeName = GetTypeIdentifier(namedType);
				if (!graph.Nodes.Contains(typeName))
				{
					graph.Nodes.Add(typeName);
				}
			}
			else
			{
				foreach (var type in namedType.GetMembers())
				{
					AddNodeIfNotExists(type as INamespaceOrTypeSymbol, graph, typeSymbols);
				}
			}
		}

		private string GetTypeIdentifier(INamespaceOrTypeSymbol symbol)
		{
			if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
			{
				return symbol.MetadataName;
			}

			var parentName = GetTypeIdentifier(symbol.ContainingNamespace);

			return parentName + "." + symbol.MetadataName;
		}

		private void AddInheritanceEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, graph.InheritanceEdges, graph);
		}

		private void AddUseEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, graph.UseEdges, graph);
		}

		private void AddEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, HashSet<Tuple<string, string>> edges, IntertypeRelationGraph graph)
		{
			if (from == null || to == null || from.TypeKind == TypeKind.Error || to.TypeKind == TypeKind.Error || Equals(from, to))
				return;

			if (to.IsGenericType)
			{
				foreach (var genericArugment in to.TypeArguments)
				{
					AddUseEdgeIfBothExist(from, genericArugment as INamedTypeSymbol, graph);
				}
			}
			var fromName = GetTypeIdentifier(from);
			var toName = GetTypeIdentifier(to);

			if (!graph.Nodes.Contains(fromName) || !graph.Nodes.Contains(toName))
			{
				return;
			}

			if (!edges.Any(x => x.Item1 == fromName && x.Item2 == toName))
			{
				edges.Add(new Tuple<string, string>(fromName, toName));
			}
		}


		#endregion

		public Task PersistDataStructure(IntertypeRelationGraph dataStructure)
		{
			throw new NotImplementedException();
		}
	}
}