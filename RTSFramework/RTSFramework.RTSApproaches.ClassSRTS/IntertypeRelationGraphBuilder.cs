using System.Collections.Generic;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.Core.Models;

namespace RTSFramework.RTSApproaches.ClassSRTS
{
    public class IntertypeRelationGraphBuilder
    {
        private const string MonoModuleTyp = "<Module>";

        public IntertypeRelationGraph BuildIntertypeRelationGraph(IList<CSharpAssembly> assemblies)
        {
            var graph = new IntertypeRelationGraph();
            var typeDefinitions = new List<TypeDefinition>();

            //First Collect All Types as Nodes
            foreach (var assembly in assemblies)
            {
                var moduleDefinition = ModuleDefinition.ReadModule(assembly.AbsolutePath);

                foreach (var type in moduleDefinition.Types)
                {
                    if (type.Name == MonoModuleTyp)
                    {
                        continue;
                    }

                    typeDefinitions.Add(type);
                    graph.Nodes.Add(new IntertypeRelationGraphNode(type.FullName));

                    typeDefinitions.AddRange(GetNestedTypes(type, graph));
                }
            }

            //Second, Build Edges
            foreach (var type in typeDefinitions)
            {
                ProcessTypeDefinition(type, graph);
            }

            return graph;
        }

        private IList<TypeDefinition> GetNestedTypes(TypeDefinition type, IntertypeRelationGraph graph)
        {
            var types = new List<TypeDefinition>();

            if (type.HasNestedTypes)
            {
                foreach (var nestedType in type.NestedTypes)
                {
                    types.Add(nestedType);
                    graph.Nodes.Add(new IntertypeRelationGraphNode(nestedType.FullName));
                    types.AddRange(GetNestedTypes(nestedType, graph));
                }
            }

            return types;
        }

        private void ProcessTypeDefinition(TypeDefinition type, IntertypeRelationGraph graph)
        {
            if (type.BaseType != null)
            {
                graph.AddInheritanceEdgeIfBothExist(type, type.BaseType);
            }

            if (type.HasInterfaces)
            {
                foreach (var interfaceDef in type.Interfaces)
                {
                    graph.AddInheritanceEdgeIfBothExist(type, interfaceDef.InterfaceType);
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
                    graph.AddUseEdgeIfBothExist(type, attribute.AttributeType);
                }
            }
        }

        private void ProcessFieldDefinition(FieldDefinition fieldDefinition, IntertypeRelationGraph graph, TypeDefinition type)
        {
            graph.AddUseEdgeIfBothExist(type, fieldDefinition.FieldType);

            if (fieldDefinition.HasCustomAttributes)
            {
                foreach (var attribute in fieldDefinition.CustomAttributes)
                {
                    graph.AddUseEdgeIfBothExist(type, attribute.AttributeType);
                }
            }
        }

        private void ProcessGenericParameter(GenericParameter genericParameter, IntertypeRelationGraph graph, TypeDefinition type)
        {
            if (genericParameter.HasConstraints)
            {
                foreach (var constraint in genericParameter.Constraints)
                {
                    graph.AddUseEdgeIfBothExist(type, constraint);
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
            graph.AddUseEdgeIfBothExist(type, eventDefinition.EventType);

            if (eventDefinition.HasCustomAttributes)
            {
                foreach (var attribute in eventDefinition.CustomAttributes)
                {
                    graph.AddUseEdgeIfBothExist(type, attribute.AttributeType);
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
            graph.AddUseEdgeIfBothExist(currentType, propertyDefinition.PropertyType);

            if (propertyDefinition.HasCustomAttributes)
            {
                foreach (var attribute in propertyDefinition.CustomAttributes)
                {
                    graph.AddUseEdgeIfBothExist(currentType, attribute.AttributeType);
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
                    graph.AddUseEdgeIfBothExist(currentType, parameter.ParameterType);
                }
            }
            //Uses return type
            graph.AddUseEdgeIfBothExist(currentType, methodDefinition.ReturnType);

            if (methodDefinition.HasBody)
            {
                //Uses all local variable types
                foreach (var variableDefinition in methodDefinition.Body.Variables)
                {
                    graph.AddUseEdgeIfBothExist(currentType, variableDefinition.VariableType);
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
                    graph.AddUseEdgeIfBothExist(currentType, attribute.AttributeType);
                }
            }
        }
    }
}