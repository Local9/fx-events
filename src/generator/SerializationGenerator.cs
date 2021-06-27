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
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (SyntaxReceiver) context.SyntaxContextReceiver;

            if (receiver == null) return;

            foreach (var item in receiver.WorkItems)
            {
                var code = receiver.Compile(item);

                context.AddSource($"{item.TypeSymbol.Name}.Serialization.cs",
                    SourceText.From(code.ToString(), Encoding.UTF8));
            }
            
            // context.AddSource("Logs.cs",
            //     SourceText.From(
            //         $@"/*{Environment.NewLine + string.Join(Environment.NewLine, receiver.Logs) + Environment.NewLine}*/",
            //         Encoding.UTF8));
        }
    }
}