﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts.Adapter;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;
using RTSFramework.Core.Models;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
	public class CSharpFileClassDeltaAdapter : IDeltaAdapter<StructuralDelta<CSharpFilesProgramModel, CSharpFileElement>, StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>, CSharpFilesProgramModel, CSharpClassesProgramModel> 
	{
		private CSharpClassesProgramModel Convert(CSharpFilesProgramModel filesModel)
		{
			var classesModel = new CSharpClassesProgramModel
			{
				AbsoluteSolutionPath = filesModel.AbsoluteSolutionPath,
				VersionId = filesModel.VersionId,
				GetClasses = () =>
				{
					var classes = new List<CSharpClassElement>();
					foreach (var file in filesModel.Files)
					{
						classes.AddRange(GetContainedClasses(file.GetContent()));
					}

					return classes;
				}
			};

			return classesModel;
		}

		public StructuralDelta<CSharpClassesProgramModel, CSharpClassElement> Convert(StructuralDelta<CSharpFilesProgramModel, CSharpFileElement> delta)
		{
			var result = new StructuralDelta<CSharpClassesProgramModel, CSharpClassElement>(Convert(delta.OldModel), Convert(delta.NewModel));

			foreach (var cSharpFile in delta.ChangedElements)
			{
				result.ChangedElements.AddRange(GetContainedClasses(cSharpFile.GetContent()));
			}
			foreach (var cSharpFile in delta.AddedElements)
			{
				result.AddedElements.AddRange(GetContainedClasses(cSharpFile.GetContent()));
			}
			foreach (var cSharpFile in delta.DeletedElements)
			{
				result.DeletedElements.AddRange(GetContainedClasses(cSharpFile.GetContent()));
			}

			return result;
		}

		/// <summary>
		/// Note that changed interfaces can not cause tests to be impacted as tests are executed on classes not interfaces
		/// That's why interface definitions are omitted here
		/// </summary>
		/// <param name="fileContent"></param>
		/// <returns></returns>
		private IEnumerable<CSharpClassElement> GetContainedClasses(string fileContent)
		{
			var classes = new List<CSharpClassElement>();

			SyntaxTree tree = CSharpSyntaxTree.ParseText(fileContent);

			var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
			foreach (var classDeclaration in classDeclarations)
			{
				string className;
				var parentNamespace = classDeclaration.Parent as NamespaceDeclarationSyntax;
				if (parentNamespace != null)
				{
					var nameSpaceName = GetFullNamespaceName(parentNamespace);

					className = $"{nameSpaceName}.{GetClassName(classDeclaration)}";
				}
				else
				{
					className = GetClassName(classDeclaration);
				}

				classes.Add(new CSharpClassElement(className));
			}

			return classes;
		}

		private string GetClassName(ClassDeclarationSyntax classDeclaration)
		{
			var className = $"{classDeclaration.Identifier}";

			if (classDeclaration.TypeParameterList != null && classDeclaration.TypeParameterList.Parameters.Any())
			{
				className += "`" + classDeclaration.TypeParameterList.Parameters.Count;
			}
			return className;
		}

		private string GetFullNamespaceName(NamespaceDeclarationSyntax node)
		{
			var parent = node.Parent as NamespaceDeclarationSyntax;
			if (parent != null)
				return $"{GetFullNamespaceName(parent)}.{GetNamespaceName(node)}";
			return GetNamespaceName(node);
		}

		private string GetNamespaceName(NamespaceDeclarationSyntax node)
		{
			return node.Name.ToString();
		}

		
	}
}
