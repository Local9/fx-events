using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Lusive.Events.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Lusive.Events.Tests
{
    public class Tests
    {
        private readonly ITestOutputHelper _output;

        public Tests(ITestOutputHelper output)
        {
            _output = output;
            
            Console.SetOut(new ConsoleSink(output));
        }
        
        [Fact]
        public void Serialization()
        {
            var compilation = CreateCompilation(@"
using System;

namespace Lusive.Events.Tests
{
    public class SerializationAttribute : Attribute
    {
    }

    [Serialization]
    public partial class DummyType : BaseType
    {
        public string ReadonlyName { get; }
    }

    [Serialization]
    public partial class BaseType 
    {
    }
}
");
            var result = RunGenerators(compilation, out var diagnostics, new SerializationGenerator());

            Assert.Empty(diagnostics);
            Assert.Empty(result.GetDiagnostics());
            Assert.True(true);
        }

        private static Compilation CreateCompilation(string source) => CSharpCompilation.Create(
            "compilation",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators) =>
            CSharpGeneratorDriver.Create(
                ImmutableArray.Create(generators),
                ImmutableArray<AdditionalText>.Empty,
                (CSharpParseOptions) compilation.SyntaxTrees.First().Options
            );

        private static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics,
            params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators)
                .RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);

            return updatedCompilation;
        }
    }
    
    public class ConsoleSink : TextWriter
    {
        private readonly ITestOutputHelper _output;

        public ConsoleSink(ITestOutputHelper output)
        {
            _output = output;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string message)
        {
            _output.WriteLine(message);
        }

        public override void WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }
    }
}