using System.Collections.Generic;
using System.Linq;
using Lusive.Events.Generator.Generation;
using Lusive.Events.Generator.Schemas;
using Lusive.Events.Generator.Serialization.Generation;
using Lusive.Events.Generator.Serialization.Instructions;
using Lusive.Events.Generator.Syntax;

namespace Lusive.Events.Generator.Serialization
{
    public class InstructionHost
    {
        public GenerationHost GenerationHost { get; }

        public InstructionHost(GenerationHost generationHost)
        {
            GenerationHost = generationHost;
        }

        public void Build(GenerationContext context, BeginInstruction beginInstruction, CodeWriter code)
        {
            if (beginInstruction.Children != null)
            {
                Build(context, beginInstruction.Children, SchemaMember.Empty, code);
            }

            foreach (var member in beginInstruction.Members.ToArray())
            {
                Build(context, member.Item2, member.Item1, code);
            }
        }

        public void Build(GenerationContext context, IEnumerable<BaseInstruction> instructions, SchemaMember member,
            CodeWriter code)
        {
            var scope = code.CurrentScope;

            foreach (var instruction in instructions.ToArray())
            {
                if (instruction is BeginInstruction beginInstruction)
                {
                    Build(context, beginInstruction, code);

                    continue;
                }

                instruction.Build(context, this, member, code);

                if (instruction.Children == null) continue;

                using (code.Scope())
                {
                    Build(context, instruction.Children, member, code);
                }
            }

            while (code.CurrentScope > scope)
            {
                code.Close();
            }
        }
    }
}