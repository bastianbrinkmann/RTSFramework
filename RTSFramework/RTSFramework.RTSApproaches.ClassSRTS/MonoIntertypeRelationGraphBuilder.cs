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
	public class MonoIntertypeRelationGraphBuilder<TCSharpModel> : IDataStructureProvider<IntertypeRelationGraph, TCSharpModel> where TCSharpModel : CSharpProgramModel
	{
		private readonly IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter;

		public MonoIntertypeRelationGraphBuilder(IArtefactAdapter<string, IList<CSharpAssembly>> assembliesArtefactAdapter)
		{
			this.assembliesArtefactAdapter = assembliesArtefactAdapter;
		}

		private const string MonoModuleTyp = "<Module>";

		public Task<IntertypeRelationGraph> GetDataStructureForProgram(TCSharpModel sourceModel, CancellationToken cancellationToken)
		{
			var assemblies = assembliesArtefactAdapter.Parse(sourceModel.AbsoluteSolutionPath);

			var graph = new IntertypeRelationGraph();
			var typeDefinitions = new List<TypeDefinition>();

			//First Collect All Types as Nodes
			foreach (var assembly in assemblies)
			{
				if (!File.Exists(assembly.AbsolutePath))
				{
					continue;
				}

				ModuleDefinition moduleDefinition = null;

				TrackAverageTimes("LoadingModuleDefinition", () =>
				{
					moduleDefinition = ModuleDefinition.ReadModule(assembly.AbsolutePath);
				});

				TrackAverageTimes("AddingNodes", () =>
				{
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
				});
			}

			//Second, Build Edges
			foreach (var type in typeDefinitions)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return null;
				}
				ProcessTypeDefinition(type, graph);
			}

			PrintTrackedTimes();

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

				Debug.WriteLine($"{name.PadRight(30)}: {averageTimeString.PadRight(25)} * {executionsString.PadRight(10)} = {averageTime*executions}");
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

			double averageTime = averageTimesDictionary[AverageTimeKey(name)];
			double numberOfExecutions = averageTimesDictionary[NumberOfExecutionsKey(name)];
			double totalTime = averageTime * numberOfExecutions;

			var stopWatch = new Stopwatch();
			stopWatch.Start();
			action();
			stopWatch.Stop();

			totalTime += stopWatch.Elapsed.TotalSeconds;
			numberOfExecutions += 1;

			averageTimesDictionary[AverageTimeKey(name)] = totalTime / numberOfExecutions;
			averageTimesDictionary[NumberOfExecutionsKey(name)] = numberOfExecutions;
		}

		#endregion

		private void ProcessTypeDefinition(TypeDefinition type, IntertypeRelationGraph graph)
		{
			if (type.BaseType != null)
			{
				TrackAverageTimes("BaseType", () =>
				{
					AddInheritanceEdgeIfBothExist(type, type.BaseType, graph);
				});
			}
			if (type.HasInterfaces)
			{
				TrackAverageTimes("Interfaces", () =>
				{
					foreach (var interfaceDef in type.Interfaces)
					{
						AddInheritanceEdgeIfBothExist(type, interfaceDef.InterfaceType, graph);
					}
				});
			}
			if (type.HasMethods)
			{
				
				foreach (var method in type.Methods)
				{
					TrackAverageTimes("Method", () =>
					{
						ProcessMethodDefinition(method, graph, type);
					});
				}
				
			}
			if (type.HasProperties)
			{
				foreach (var property in type.Properties)
				{
					TrackAverageTimes("Property", () =>
					{
						ProcessPropertyDefinition(property, graph, type);
					});
				}
			}
			if (type.HasFields)
			{
				foreach (var field in type.Fields)
				{
					TrackAverageTimes("Field", () =>
					{
						ProcessFieldDefinition(field, graph, type);
					});
				}
			}
			if (type.HasEvents)
			{
				foreach (var eventDef in type.Events)
				{
					TrackAverageTimes("Event", () =>
					{
						ProcessEventDefinition(eventDef, graph, type);
					});
				}
			}
			if (type.HasGenericParameters)
			{
				TrackAverageTimes("GenericParameters", () =>
				{
					foreach (var genericParameter in type.GenericParameters)
					{
						ProcessGenericParameter(genericParameter, graph, type);
					}
				});
			}
			if (type.HasCustomAttributes)
			{
				TrackAverageTimes("CustomAttributes", () =>
				{
					foreach (var attribute in type.CustomAttributes)
					{
						AddUseEdgeIfBothExist(type, attribute.AttributeType, graph);
					}
				});
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
					TrackAverageTimes("Instruction", () =>
					{
						ProcessInstruction(instruction, graph, currentType);
					});
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
			AddEdgeIfBothExist(from, to, graph.InheritanceEdges, graph);
		}

		private void AddUseEdgeIfBothExist(TypeDefinition from, TypeReference to, IntertypeRelationGraph graph)
		{
			AddEdgeIfBothExist(from, to, graph.UseEdges, graph);
		}

		private void AddEdgeIfBothExist(TypeDefinition from, TypeReference to, HashSet<Tuple<string, string>> edges, IntertypeRelationGraph graph)
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

			if (!edges.Any(x => x.Item1 == from.FullName && x.Item2 == to.FullName))
			{
				edges.Add(new Tuple<string, string>(from.FullName, to.FullName));
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

		public Task PersistDataStructure(IntertypeRelationGraph dataStructure)
		{
			throw new NotImplementedException();
		}
	}
}