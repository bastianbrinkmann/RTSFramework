using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.Utilities;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
	public class RoslynCompiledIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureProvider<IntertypeRelationGraph, TCSharpModel>
		where TCSharpModel : CSharpProgramModel
	{
		private readonly ISettingsProvider settingsProvider;
		private List<Compilation> compilations;

		public RoslynCompiledIntertypeRelationGraphBuilder(ISettingsProvider settingsProvider)
		{
			this.settingsProvider = settingsProvider;
		}

		public async Task<IntertypeRelationGraph> GetDataStructure(TCSharpModel model, CancellationToken token)
		{
			var graph = new IntertypeRelationGraph();
			var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
			{
				{ "Configuration", settingsProvider.Configuration },
				{ "Platform", settingsProvider.Platform }
			});

			var solution = await workspace.OpenSolutionAsync(model.AbsoluteSolutionPath, token);

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
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = token,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			Parallel.ForEach(typeSymbols, parallelOptions, type =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				ProcessTypeSymbol(type, graph);
			});

			return graph;
		}

		private void ProcessTypeSymbol(INamedTypeSymbol type, IntertypeRelationGraph graph)
		{
			if (type.BaseType != null)
			{
				AddInheritanceEdgeIfBothExist(type, type.BaseType, graph);
			}

			foreach (var typeInterface in type.Interfaces)
			{
				AddInheritanceEdgeIfBothExist(type, typeInterface, graph);
			}

			ProcessAttributes(type, type, graph);

			foreach (var symbol in type.GetMembers())
			{
				ProcessAttributes(type, symbol, graph);

				foreach (var typeParameter in type.TypeParameters)
				{
					foreach (var constraint in typeParameter.ConstraintTypes)
					{
						AddUseEdgeIfBothExist(type, constraint as INamedTypeSymbol, graph);
					}
				}

				var method = symbol as IMethodSymbol;
				if (method != null)
				{
					ProcessMethodSymbol(type, method, graph);
				}

				var property = symbol as IPropertySymbol;
				if (property != null)
				{
					ProcessPropertySymbol(type, property, graph);
				}

				var field = symbol as IFieldSymbol;
				if (field != null)
				{
					ProcessFieldSymbol(type, field, graph);
				}

				var eventSymbol = symbol as IEventSymbol;
				if (eventSymbol != null)
				{
					ProcessEventSymbol(type, eventSymbol, graph);
				}
			}
		}

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
		}

		private void ProcessOperations(SemanticModel semanticModel, INamedTypeSymbol type, IntertypeRelationGraph graph, SyntaxNode node)
		{
			IOperation operation = semanticModel.GetOperation(node);

			ProcessOperation(type, operation, graph);
		}

		private void ProcessOperation(INamedTypeSymbol type, IOperation operation, IntertypeRelationGraph graph)
		{
			foreach (var child in operation.Children)
			{
				ProcessOperation(type, child, graph);
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
				case OperationKind.ObjectCreation:
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
				case OperationKind.FieldInitializer:
					var fieldInit = (IFieldInitializerOperation)operation;
					foreach (var initializedField in fieldInit.InitializedFields)
					{
						AddUseEdgeIfBothExist(type, initializedField.Type as INamedTypeSymbol, graph);
					}
					break;
				case OperationKind.PropertyInitializer:
					var propertyInit = (IPropertyInitializerOperation)operation;
					foreach (var initProperty in propertyInit.InitializedProperties)
					{
						AddUseEdgeIfBothExist(type, initProperty.Type as INamedTypeSymbol, graph);
					}
					break;
				case OperationKind.ParameterInitializer:
					var paramInit = (IParameterInitializerOperation)operation;
					AddUseEdgeIfBothExist(type, paramInit.Parameter.ContainingType, graph);
					break;
				case OperationKind.VariableDeclarator:
					var variableDeclarator = (IVariableDeclaratorOperation)operation;
					AddUseEdgeIfBothExist(type, variableDeclarator.Symbol.Type as INamedTypeSymbol, graph);
					break;
				case OperationKind.Argument:
					var argumentOperation = (IArgumentOperation)operation;
					AddUseEdgeIfBothExist(type, argumentOperation.Parameter.Type as INamedTypeSymbol, graph);
					break;
				case OperationKind.CatchClause:
					var catchClause = (ICatchClauseOperation)operation;
					AddUseEdgeIfBothExist(type, catchClause.ExceptionType as INamedTypeSymbol, graph);
					break;
				case OperationKind.DeclarationPattern:
					var declarationpattern = (IDeclarationPatternOperation)operation;
					AddUseEdgeIfBothExist(type, declarationpattern.DeclaredSymbol as INamedTypeSymbol, graph);
					break;
				case OperationKind.Invocation:
					var incovation = (IInvocationOperation)operation;
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
			AddEdgeIfBothExist(from, to, false, graph);
		}

		private void AddUseEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, true, graph);
		}

		private void AddEdgeIfBothExist(INamedTypeSymbol from, INamedTypeSymbol to, bool useEdge, IntertypeRelationGraph graph)
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

			if (useEdge)
			{
				graph.AddUseEdgeIfNotExists(fromName, toName);
			}
			else
			{
				graph.AddInheritanceEdgeIfNotExists(fromName, toName);
			}
		}

		#endregion

		public Task PersistDataStructure(IntertypeRelationGraph dataStructure)
		{
			throw new NotImplementedException();
		}
	}
}