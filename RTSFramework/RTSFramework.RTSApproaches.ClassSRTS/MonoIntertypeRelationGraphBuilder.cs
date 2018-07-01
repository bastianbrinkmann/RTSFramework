using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.RTSApproaches.Core.Contracts;
using RTSFramework.RTSApproaches.Core.DataStructures;

namespace RTSFramework.RTSApproaches.Static
{
	public class MonoIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureBuilder<IntertypeRelationGraph, TCSharpModel> where TCSharpModel : CSharpProgramModel
	{
		private readonly IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter;

		public MonoIntertypeRelationGraphBuilder(IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter)
		{
			this.assembliesArtefactAdapter = assembliesArtefactAdapter;
		}

		private const string MonoModuleTyp = "<Module>";

		public Task<IntertypeRelationGraph> GetDataStructure(TCSharpModel model, CancellationToken cancellationToken)
		{
			var assemblies = assembliesArtefactAdapter.Parse(model.AbsoluteSolutionPath);

			var graph = new IntertypeRelationGraph();
			var typeDefinitions = new List<TypeDefinition>();

			//First Collect All Types as Nodes
			foreach (var assembly in assemblies)
			{
				if (!File.Exists(assembly.AbsolutePath))
				{
					continue;
				}

				var moduleDefinition = ModuleDefinition.ReadModule(assembly.AbsolutePath);
				foreach (var type in moduleDefinition.Types)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (type.Name == MonoModuleTyp)
					{
						continue;
					}

					AddNodeIfNotAlreadyThere(type, graph, typeDefinitions);

					AddNestedTypes(type, graph, typeDefinitions);
				}
			}

			//Second, Build Edges
			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};
			Parallel.ForEach(typeDefinitions, parallelOptions, type =>
			{
				parallelOptions.CancellationToken.ThrowIfCancellationRequested();
				ProcessTypeDefinition(type, graph);
			});

			return Task.FromResult(graph);
		}

		private void AddNestedTypes(TypeDefinition type, IntertypeRelationGraph graph, List<TypeDefinition> typeDefinitions)
		{
			if (type.HasNestedTypes)
			{
				foreach (var nestedType in type.NestedTypes)
				{
					AddNodeIfNotAlreadyThere(nestedType, graph, typeDefinitions);
					AddNestedTypes(nestedType, graph, typeDefinitions);
				}
			}
		}

		private void ProcessTypeDefinition(TypeDefinition type, IntertypeRelationGraph graph)
		{
			if (type.BaseType != null)
			{
				AddInheritanceEdgeIfBothExist(type, type.BaseType, graph);
			}
			if (type.HasInterfaces)
			{
				foreach (var interfaceDef in type.Interfaces)
				{
					AddInheritanceEdgeIfBothExist(type, interfaceDef.InterfaceType, graph);
				}
			}
			if (type.HasMethods)
			{

				foreach (var method in type.Methods)
				{
					ProcessMethodDefinition(method, graph, type);
				}

			}
			if (type.HasProperties)
			{
				foreach (var property in type.Properties)
				{
					ProcessPropertyDefinition(property, graph, type);
				}
			}
			if (type.HasFields)
			{
				foreach (var field in type.Fields)
				{
					ProcessFieldDefinition(field, graph, type);
				}
			}
			if (type.HasEvents)
			{
				foreach (var eventDef in type.Events)
				{
					ProcessEventDefinition(eventDef, graph, type);
				}
			}
			if (type.HasGenericParameters)
			{
				foreach (var genericParameter in type.GenericParameters)
				{
					ProcessGenericParameter(genericParameter, graph, type);
				}
			}
			if (type.HasCustomAttributes)
			{
				foreach (var attribute in type.CustomAttributes)
				{
					AddUseEdgeIfBothExist(type, attribute.AttributeType, graph);
				}
			}
		}

		private void ProcessFieldDefinition(FieldDefinition fieldDefinition, IntertypeRelationGraph graph, TypeDefinition type)
		{
			AddUseEdgeIfBothExist(type, fieldDefinition.FieldType, graph);

			if (fieldDefinition.HasCustomAttributes)
			{
				foreach (var attribute in fieldDefinition.CustomAttributes)
				{
					AddUseEdgeIfBothExist(type, attribute.AttributeType, graph);
				}
			}
		}

		private void ProcessGenericParameter(GenericParameter genericParameter, IntertypeRelationGraph graph, TypeDefinition type)
		{
			if (genericParameter.HasConstraints)
			{
				foreach (var constraint in genericParameter.Constraints)
				{
					AddUseEdgeIfBothExist(type, constraint, graph);
				}
			}
		}

		private void ProcessEventDefinition(EventDefinition eventDefinition, IntertypeRelationGraph graph, TypeDefinition type)
		{
			if (eventDefinition.AddMethod != null)
			{
				ProcessMethodDefinition(eventDefinition.AddMethod, graph, type);
			}
			if (eventDefinition.RemoveMethod != null)
			{
				ProcessMethodDefinition(eventDefinition.RemoveMethod, graph, type);
			}
			if (eventDefinition.InvokeMethod != null)
			{
				ProcessMethodDefinition(eventDefinition.InvokeMethod, graph, type);
			}
			if (eventDefinition.HasOtherMethods)
			{
				foreach (var methodDef in eventDefinition.OtherMethods)
				{
					ProcessMethodDefinition(methodDef, graph, type);
				}
			}
			AddUseEdgeIfBothExist(type, eventDefinition.EventType, graph);

			if (eventDefinition.HasCustomAttributes)
			{
				foreach (var attribute in eventDefinition.CustomAttributes)
				{
					AddUseEdgeIfBothExist(type, attribute.AttributeType, graph);
				}
			}
		}

		private void ProcessPropertyDefinition(PropertyDefinition propertyDefinition, IntertypeRelationGraph graph, TypeDefinition currentType)
		{
			if (propertyDefinition.SetMethod != null)
			{
				ProcessMethodDefinition(propertyDefinition.SetMethod, graph, currentType);
			}
			if (propertyDefinition.GetMethod != null)
			{
				ProcessMethodDefinition(propertyDefinition.GetMethod, graph, currentType);
			}
			AddUseEdgeIfBothExist(currentType, propertyDefinition.PropertyType, graph);

			if (propertyDefinition.HasCustomAttributes)
			{
				foreach (var attribute in propertyDefinition.CustomAttributes)
				{
					AddUseEdgeIfBothExist(currentType, attribute.AttributeType, graph);
				}
			}
		}

		private void ProcessMethodDefinition(MethodDefinition methodDefinition, IntertypeRelationGraph graph, TypeDefinition currentType)
		{
			if (methodDefinition.HasParameters)
			{
				//Uses all parameter types
				foreach (var parameter in methodDefinition.Parameters)
				{
					AddUseEdgeIfBothExist(currentType, parameter.ParameterType, graph);
				}
			}
			//Uses return type
			AddUseEdgeIfBothExist(currentType, methodDefinition.ReturnType, graph);

			if (methodDefinition.HasBody)
			{
				//Uses all local variable types
				foreach (var variableDefinition in methodDefinition.Body.Variables)
				{
					AddUseEdgeIfBothExist(currentType, variableDefinition.VariableType, graph);
				}

				foreach (var instruction in methodDefinition.Body.Instructions)
				{
					ProcessInstruction(instruction, graph, currentType);
				}
			}

			if (methodDefinition.HasGenericParameters)
			{
				foreach (var genericParameter in methodDefinition.GenericParameters)
				{
					ProcessGenericParameter(genericParameter, graph, currentType);
				}
			}

			if (methodDefinition.HasCustomAttributes)
			{
				foreach (var attribute in methodDefinition.CustomAttributes)
				{
					AddUseEdgeIfBothExist(currentType, attribute.AttributeType, graph);
				}
			}
		}

		private void ProcessInstruction(Instruction instruction, IntertypeRelationGraph graph, TypeDefinition currentType)
		{
			if (instruction.Operand == null)
				return;

			var typeRef = instruction.Operand as TypeReference;
			if (typeRef != null)
			{
				AddUseEdgeIfBothExist(currentType, typeRef, graph);
			}

			var memberRef = instruction.Operand as MemberReference;
			if (memberRef != null)
			{
				AddUseEdgeIfBothExist(currentType, memberRef.DeclaringType, graph);
			}
		}

		private void AddInheritanceEdgeIfBothExist(TypeDefinition from, TypeReference to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, false, graph);
		}

		private void AddUseEdgeIfBothExist(TypeDefinition from, TypeReference to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, true, graph);
		}

		private void AddEdgeIfBothExist(TypeDefinition from, TypeReference to, bool useEdge, IntertypeRelationGraph graph)
		{
			if (from == null || to == null)
				return;

			var genericType = to as GenericInstanceType;
			if (genericType != null && genericType.HasGenericArguments)
			{
				foreach (var genericArgument in genericType.GenericArguments)
				{
					AddUseEdgeIfBothExist(from, genericArgument, graph);
				}
			}

			if (!graph.Nodes.Contains(from.FullName) || !graph.Nodes.Contains(to.FullName))
			{
				return;
			}

			if (useEdge)
			{
				graph.AddUseEdgeIfNotExists(from.FullName, to.FullName);
			}
			else
			{
				graph.AddInheritanceEdgeIfNotExists(from.FullName, to.FullName);
			}
		}

		private void AddNodeIfNotAlreadyThere(TypeDefinition type, IntertypeRelationGraph graph, List<TypeDefinition> typeDefinitions)
		{
			if (!graph.Nodes.Contains(type.FullName) && !type.IsAnonymousType())
			{
				typeDefinitions.Add(type);

				graph.Nodes.Add(type.FullName);
			}
		}
	}
}