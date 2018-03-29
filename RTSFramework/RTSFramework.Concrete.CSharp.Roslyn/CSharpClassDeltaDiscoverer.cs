using System.Collections.Generic;
using System.IO;
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
    public class CSharpClassDeltaDiscoverer<TP> : IOfflineDeltaDiscoverer<TP, StructuralDelta<CSharpClassElement>>
        where TP : IProgramModel
    {
        private readonly IOfflineDeltaDiscoverer<TP, StructuralDelta<CSharpFileElement>> internalDiscoverer;
        private readonly IFilesProvider<TP> filesProvider;

        public CSharpClassDeltaDiscoverer(IOfflineDeltaDiscoverer<TP, StructuralDelta<CSharpFileElement>> internalDiscoverer,
            IFilesProvider<TP> filesProvider)
        {
            this.internalDiscoverer = internalDiscoverer;
            this.filesProvider = filesProvider;
        }

        public StructuralDelta<CSharpClassElement> Discover(TP oldModel, TP newModel)
        {
            var fileDelta = internalDiscoverer.Discover(oldModel, newModel);
            return Convert(fileDelta, oldModel);
        }

        private StructuralDelta<CSharpClassElement> Convert(StructuralDelta<CSharpFileElement> delta, TP oldModel)
        {
            StructuralDelta<CSharpClassElement> result = new StructuralDelta<CSharpClassElement>
            {
                SourceModelId = delta.SourceModelId,
                TargetModelId = delta.TargetModelId,
            };

            foreach (var cSharpFile in delta.ChangedElements)
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(cSharpFile.Id)))
                {
                    result.ChangedElements.AddRange(GetContainedClasses(reader.ReadToEnd()));
                }
            }
            foreach (var cSharpFile in delta.AddedElements)
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(cSharpFile.Id)))
                {
                    result.AddedElements.AddRange(GetContainedClasses(reader.ReadToEnd()));
                }
            }
            foreach (var cSharpFile in delta.DeletedElements)
            {
                result.DeletedElements.AddRange(GetContainedClasses(filesProvider.GetFileContent(oldModel, cSharpFile.Id)));
            }

            return result;
        }

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

        public static string GetFullNamespaceName(NamespaceDeclarationSyntax node)
        {
            var parent = node.Parent as NamespaceDeclarationSyntax;
            if (parent != null)
                return $"{GetFullNamespaceName(parent)}.{((IdentifierNameSyntax)node.Name).Identifier}";
            return ((IdentifierNameSyntax)node.Name).Identifier.ToString();
        }
    }
}
