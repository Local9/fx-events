using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Moonlight.Generators
{
    [Generator]
    public class SerializationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SerializationEngine());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var engine = (SerializationEngine) context.SyntaxContextReceiver;

            if (engine == null) return;

            foreach (var item in engine.WorkItems)
            {
                var code = engine.Compile(item);

                foreach (var problem in engine.Problems)
                {
                    Location location = null;

                    foreach (var entry in problem.Locations)
                    {
                        location = entry;

                        if (!location.IsInMetadata)
                        {
                            break;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(problem.Descriptor, location, problem.Format));
                }

                engine.Problems.Clear();
                context.AddSource($"{item.TypeSymbol.Name}.Serialization.cs",
                    SourceText.From(code.ToString(), Encoding.UTF8));
            }

            // context.AddSource("Logs.cs",
            //     SourceText.From(
            //         $@"/*{Environment.NewLine + string.Join(Environment.NewLine, engine.Logs) + Environment.NewLine}*/",
            //         Encoding.UTF8));
        }
    }
}