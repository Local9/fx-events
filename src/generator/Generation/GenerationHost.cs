using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Models;
using Lusive.Events.Generator.Problems;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization;
using Lusive.Events.Generator.Serialization.Converter;
using Lusive.Events.Generator.Serialization.Converter.Special;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Identifiers;
using Lusive.Events.Generator.Serialization.Instructions;
using Lusive.Events.Generator.Syntax;
using Microsoft.CodeAnalysis;

namespace Lusive.Events.Generator.Generation
{
    public class GenerationHost
    {
        private static readonly BaseConverter[] Converters =
        {
            new TimeSerialization(),
            new KeyValuePairSerialization(),
            new TupleSerialization(),
            new ArraySerialization(),
            new EnumerableSerialization(),
            new EnumSerialization()
        };

        private readonly GenerationEngine _engine;
        private readonly InstructionHost _instructionHost;

        public GenerationHost(GenerationEngine engine)
        {
            _engine = engine;
            _instructionHost = new InstructionHost(this);
        }

        public CodeWriter CreateFile(WorkItem item)
        {
            var code = new CodeWriter();
            var type = item.TypeSymbol;

            Logger.Info($"[Host] Work Item: {item.TypeSymbol.Name}");

            using (Logger.Scope())
            {
                code.AppendLine("using System;");
                code.AppendLine("using System.IO;");
                code.AppendLine("using System.Linq;");

                using (code.Scope($"namespace {item.NamespaceDeclaration.Name}"))
                {
                    using (code.Scope(
                        $"public partial class {item.ClassDeclaration.Identifier}{item.ClassDeclaration.TypeParameterList} {item.ClassDeclaration.ConstraintClauses}"))
                    {
                        if (!TypeHelper.HasEmptyConstructor(item.ClassDeclaration))
                        {
                            Logger.Info("Creating empty constructor");

                            code.AppendLine($"public {type.Name}()");
                            code.Scope().Dispose();
                        }

                        using (code.Scope($"public {type.Name}(BinaryReader reader)"))
                        {
                            code.AppendLine($"{GeneratorConstant.UnpackingMethod}(reader);");
                        }

                        string GetMethodSignature(string method, string? returnType = "void")
                        {
                            var exists = TypeHelper.HasMethod(type, method);

                            if (!exists && type.BaseType != null && TypeHelper.IsSerializable(type.BaseType))
                            {
                                exists = true;
                            }

                            Logger.Info($"Method: {method} (Override = {exists})");

                            return exists ? $"new {returnType} {method}" : $"{returnType} {method}";
                        }

                        var schema = SchemaManager.BuildSchema(type);
                        var writerIdentifier = Identifier.CreateReferencingIdentifier("writer");
                        var readerIdentifier = Identifier.CreateReferencingIdentifier("reader");

                        Tuple<GenerationContext, IEnumerable<Tuple<SchemaMember, BaseInstruction[]>>> GenerateFlow(
                            Language language, SerializationFlow flow, Identifier? baseIdentifier = null)
                        {
                            var generation = new GenerationContext(language, flow, writerIdentifier, readerIdentifier);
                            var instructions = Generate(schema, generation, baseIdentifier);

                            return Tuple.Create(generation, instructions);
                        }

                        void BuildFlow(
                            Tuple<GenerationContext, IEnumerable<Tuple<SchemaMember, BaseInstruction[]>>> generated)
                        {
                            var (context, members) = generated;
                            var beginInstruction = new BeginInstruction(members);

                            _instructionHost.Build(context, beginInstruction, code);
                        }

                        using (code.Scope(
                            $"public {GetMethodSignature(GeneratorConstant.PackingMethod)}(BinaryWriter writer)"))
                        {
                            code.AppendLine(GeneratorConstant.Notice);

                            BuildFlow(GenerateFlow(Language.CSharp, SerializationFlow.Write));
                        }

                        using (code.Scope(
                            $"public {GetMethodSignature(GeneratorConstant.UnpackingMethod)}(BinaryReader reader)"))
                        {
                            code.AppendLine(GeneratorConstant.Notice);

                            BuildFlow(GenerateFlow(Language.CSharp, SerializationFlow.Read));
                        }

                        using (var memory = new MemoryStream())
                        {
                            var writer = new BinaryWriter(memory);

                            writer.Write(schema.Members.Length);

                            var flows = new[] { SerializationFlow.Write, SerializationFlow.Read };
                            var generated = new Dictionary<SchemaMember, IEnumerable<BaseInstruction>[]>();
                            var instanceIdentifier = Identifier.CreateReferencingIdentifier("instance");

                            foreach (var member in schema.Members)
                            {
                                generated.Add(member, new IEnumerable<BaseInstruction>[flows.Length]);
                            }

                            foreach (var flow in flows)
                            {
                                var entry = GenerateFlow(Language.JavaScript, flow, instanceIdentifier);

                                foreach (var member in entry.Item2)
                                {
                                    generated[member.Item1][(int) flow] = member.Item2;
                                }
                            }

                            foreach (var flow in generated.SelectMany(member => member.Value))
                            {
                                var instructions = flow != null
                                    ? flow as BaseInstruction[] ?? flow.ToArray()
                                    : Array.Empty<BaseInstruction>();

                                writer.Write(instructions.Length);

                                foreach (var instruction in instructions)
                                {
                                    instruction.WriteBuffer(writer);
                                }
                            }

                            var array = memory.ToArray();

                            using (code.Scope(
                                $"public {GetMethodSignature(GeneratorConstant.InstructionBufferMethod, "byte[]")}()"))
                            {
                                code.AppendLine(GeneratorConstant.Notice);
                                code.AppendLine($"return new byte[] {{ {string.Join(", ", array)} }};");
                            }
                        }
                    }
                }
            }

            return code;
        }

        private IEnumerable<Tuple<SchemaMember, BaseInstruction[]>> Generate(Schema schema,
            GenerationContext generation,
            Identifier? baseIdentifier = null)
            => Generate(schema, generation,
                (member, _) => baseIdentifier != null
                    ? Identifier.CreateDerivedIdentifier(baseIdentifier, member.Name)
                    : Identifier.CreateReferencingIdentifier(member.Name));

        public IEnumerable<Tuple<SchemaMember, BaseInstruction[]>> Generate(Schema schema, GenerationContext generation,
            Func<SchemaMember, int, Identifier>? identifierCreator = null)
        {
            IdentifierContext.Reset();

            var flow = generation.Flow;
            var index = 0;
            var result = new List<Tuple<SchemaMember, BaseInstruction[]>>();

            Logger.Info($"Generating Instructions: {schema.Name} ({Enum.GetName(typeof(SerializationFlow), flow)})");

            using (Logger.Scope())
            {
                foreach (var member in schema.Members.Where(
                    self => flow == SerializationFlow.Write ? self.Write : self.Read))
                {
                    var identifier = identifierCreator?.Invoke(member, index) ??
                                     Identifier.CreateReferencingIdentifier(member.Name);
                    var context = new TypeSerializationContext(generation, member, identifier);
                    var converter = GetConverter(member.TypeSymbol);
                    var instructions = GenerateInstructions(generation.Language, flow, context, converter);
                    var first = new BaseInstruction[]
                    {
                        new CommentInstruction(
                            $"Member: {identifier} ({member.TypeSymbol}, {Enum.GetName(typeof(DataType), member.DataType)}, {converter?.GetType().Name ?? "Primitive"})"),
                        new BlockInstruction()
                    };

                    Logger.Info(
                        $"Member: {member.Name}, {member.TypeSymbol.Name}, {Enum.GetName(typeof(DataType), member.DataType)} (Write = {member.Write}, Read = {member.Read})");

                    index++;
                    result.Add(Tuple.Create(member, first.Concat(instructions).ToArray()));
                }
            }

            return result;
        }

        public BaseInstruction[] GenerateInstructions(Language language, SerializationFlow flow,
            TypeSerializationContext context, BaseConverter? converter = null)
        {
            try
            {
                converter ??= GetConverter(context.Type);

                var instructions = new List<BaseInstruction>();

                if (context.IsTypeNullable || context.Type.TypeKind is TypeKind.Class)
                {
                    instructions.Add(new NullCheckInstruction(context.GetIdentifier(flow),
                        TypeHelper.IsTypeNullableType(context.OriginalType)));
                }

                if (converter == null)
                {
                    var primitive = flow switch
                    {
                        SerializationFlow.Write => new BaseInstruction[]
                            { new WriteInstruction(context.ValueIdentifier, context.TypeIdentifier) },
                        SerializationFlow.Read => new BaseInstruction[]
                            { new ReadInstruction(context.RefIdentifier, context.TypeIdentifier) },
                        _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
                    };

                    instructions.AddRange(primitive);
                }
                else
                {
                    var deep = flow switch
                    {
                        SerializationFlow.Write => converter.Write(language, this, context),
                        SerializationFlow.Read => converter.Read(language, this, context),
                        _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
                    };

                    instructions.AddRange(deep);
                }

                return instructions.ToArray();
            }
            catch (Exception exception)
            {
                var trace = exception.StackTrace;
                var culprit = ExceptionHelper.GetCulprit(trace);

                _engine.Problems.Add(new SerializationProblem
                {
                    Descriptor = new DiagnosticDescriptor(ProblemId.ExceptionOccured, "Problem Occured",
                        $"{context.Member.Name}: {exception.Message} @ {culprit}", "serialization",
                        DiagnosticSeverity.Error, true),
                    Locations = new[] { context.Member.Location }
                });

                Logger.Info(exception.ToString());

                return Array.Empty<BaseInstruction>();
            }
        }

        private static BaseConverter? GetConverter(ITypeSymbol type)
        {
            if (TypeHelper.IsPrimitive(type))
            {
                return null;
            }

            var qualified = TypeHelper.GetQualifiedName(type);
            var dataType = TypeHelper.GetDataType(type);

            foreach (var converter in Converters)
            {
                if (converter.Criteria(dataType, type, qualified))
                {
                    return converter;
                }
            }

            return type.TypeKind is TypeKind.Class or TypeKind.Struct
                ? new ObjectSerialization()
                : throw new Exception($"Could not find converter for type: {type.Name}");
        }
    }
}