using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RTSFramework.Concrete.CSharp.Core.Models;
using RTSFramework.Concrete.CSharp.Roslyn.Models;
using RTSFramework.Contracts;
using RTSFramework.Contracts.DeltaDiscoverer;
using RTSFramework.Contracts.Models;
using RTSFramework.Contracts.Models.Delta;

namespace RTSFramework.Concrete.CSharp.Roslyn
{
    public class CSharpClassDeltaDiscoverer<TModel> : IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpClassElement>> where TModel : IProgramModel
    {
        private readonly IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>> internalDiscoverer;
        private readonly IFilesProvider<TModel> filesProvider;

        public CSharpClassDeltaDiscoverer(
			IOfflineDeltaDiscoverer<TModel, StructuralDelta<TModel, CSharpFileElement>> internalDiscoverer, 
			IFilesProvider<TModel> filesProvider)
        {
            this.internalDiscoverer = internalDiscoverer;
            this.filesProvider = filesProvider;
        }

        public StructuralDelta<TModel, CSharpClassElement> Discover(TModel oldModel, TModel newModel)
        {
            var fileDelta = internalDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta, oldModel);
        }

        private StructuralDelta<TModel, CSharpClassElement> Convert(StructuralDelta<TModel, CSharpFileElement> delta, TModel oldModel)
        {
            var result = new StructuralDelta<TModel, CSharpClassElement>
			{
                SourceModel = delta.SourceModel,
                TargetModel = delta.TargetModel,
            };

            foreach (var cSharpFile in delta.ChangedElements)
            {
                result.ChangedElements.AddRange(GetContainedClasses(filesProvider.GetFileContent(oldModel, cSharpFile.Id)));
            }
            foreach (var cSharpFile in delta.AddedElements)
            {
                result.AddedElements.AddRange(GetContainedClasses(filesProvider.GetFileContent(oldModel, cSharpFile.Id)));
            }
            foreach (var cSharpFile in delta.DeletedElements)
            {
                result.DeletedElements.AddRange(GetContainedClasses(filesProvider.GetFileContent(oldModel, cSharpFile.Id)));
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

                    className = $"{nameSpaceName}.{classDeclaration.Identifier}";
                }
                else
                {
                    className = classDeclaration.Identifier.ToString();
                }

                classes.Add(new CSharpClassElement(className));
            }

            return classes;
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
