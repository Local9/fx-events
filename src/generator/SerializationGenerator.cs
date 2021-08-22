using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Lusive.Events.Generator.Extensions;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Problems;
using Lusive.Events.Generator.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Lusive.Events.Generator
{
    [Generator]
    public class SerializationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            Logger.Info("Initializing...");

            context.RegisterForSyntaxNotifications(() => new GenerationEngine());

            // if (!Debugger.IsAttached)
            // {
            //     Debugger.Launch();
            // }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteInternal(GeneratorExecutionContext context)
        {
            var engine = (GenerationEngine) context.SyntaxContextReceiver;

                if (engine == null) return;

                var host = engine.Host;
                var sources = new List<string>();

                foreach (var item in engine.WorkItems.ToArray())
                {
                    CodeWriter? code = null;

                    try
                    {
                        code = host.CreateFile(item);

                        var identifier = item.TypeSymbol.Name;
                        var count = sources.Count(self => self == identifier);
                        var unique =
                            $"{identifier}{(count != 0 ? Convert.ToChar(65 + count) : string.Empty)}.Serialization.cs";

                        foreach (var problem in engine.Problems)
                        {
                            Location location = null;

                            foreach (var entry in problem.Locations)
                            {
                                location = entry;

                                if (location != null && !location.IsInMetadata)
                                {
                                    break;
                                }
                            }

                            context.ReportDiagnostic(Diagnostic.Create(problem.Descriptor, location, problem.Format));
                        }

                        engine.Problems.Clear();

                        try
                        {
                            context.AddSource(unique, SourceText.From(code.ToString(), Encoding.UTF8));
                            sources.Add(identifier);
                        }
                        catch (ArgumentException)
                        {
                            throw new Exception(
                                $"Duplicate entry '{item.TypeSymbol.ContainingNamespace}.{item.TypeSymbol.MetadataName}' ({unique})");
                        }
                    }
                    catch (Exception exception)
                    {
                        var trace = exception.StackTrace;
                        var culprit = ExceptionHelper.GetCulprit(trace);
                        
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(ProblemId.ExceptionOccured, "Serialization Problem",
                                $"{item.TypeSymbol.Name}: {exception.Message} @ {culprit}", "serialization",
                                DiagnosticSeverity.Error, true),
                            item.TypeSymbol.Locations.FirstOrDefault()));
                    }
                    finally
                    {
                        if (code != null)
                        {
                            Logger.Info(code.ToString());
                        }
                    }
                }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                ExecuteInternal(context);
            }
            catch (Exception exception)
            {
                var trace = exception.StackTrace;
                var culprit = ExceptionHelper.GetCulprit(trace);
                
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(ProblemId.ExceptionOccured, "Serialization Problem",
                        $"generator: {exception.Message} @ {culprit}", "serialization",
                        DiagnosticSeverity.Error, true), null));
            }
            finally
            {
                context.AddSource("Logs.cs", SourceText.From($"/*{Environment.NewLine}{string.Join(Environment.NewLine, Logger.Buffer.ToArray())}{Environment.NewLine}*/", Encoding.UTF8));
            }
        }
    }
}