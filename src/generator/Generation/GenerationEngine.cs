using System.Collections.Generic;
using System.Linq;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Problems;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lusive.Events.Generator.Generation
{
    public class GenerationEngine : ISyntaxContextReceiver
    {
        public readonly List<WorkItem> WorkItems = new();
        public readonly List<SerializationProblem> Problems = new();
        public GenerationHost Host { get; }

        public GenerationEngine()
        {
            Host = new GenerationHost(this);
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDecl) return;
            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol) return;
            if (!TypeHelper.IsSerializable(symbol)) return;

            var hasPartial = classDecl.Modifiers.Any(self => self.ToString() == "partial");

            if (!hasPartial)
            {
                var problem = new SerializationProblem
                {
                    Descriptor = new DiagnosticDescriptor(ProblemId.SerializationMarking, "Serialization Marking",
                        "Serialization marked type {0} is missing the partial keyword.", "serialization",
                        DiagnosticSeverity.Error, true),
                    Locations = new[] { symbol.Locations.FirstOrDefault() },
                    Format = new object[] { symbol.Name }
                };

                Problems.Add(problem);

                return;
            }

            CompilationUnitSyntax unit = null;
            NamespaceDeclarationSyntax namespaceDecl = null;
            SyntaxNode parent = classDecl;

            while ((parent = parent.Parent) != null)
            {
                switch (parent)
                {
                    case CompilationUnitSyntax syntax:
                        unit = syntax;

                        break;
                    case NamespaceDeclarationSyntax syntax:
                        namespaceDecl = syntax;

                        break;
                }
            }

            if (unit == null || namespaceDecl == null) return;

            WorkItems.Add(new WorkItem
            {
                TypeSymbol = symbol, SemanticModel = context.SemanticModel, ClassDeclaration = classDecl, Unit = unit,
                NamespaceDeclaration = namespaceDecl
            });
        }
    }
}